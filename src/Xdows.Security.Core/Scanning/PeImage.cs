using System.Buffers.Binary;

namespace Xdows.Security.Core.Scanning;

/// <summary>
/// 最小化的 PE 解析器，用于替换原引擎中依赖 <c>ImageRvaToVa</c> + 内联汇编的导入表 / 导出表读取。
/// 字段偏移遵循 PE Format 规范（Windows SDK 中的 IMAGE_* 结构）。
/// </summary>
internal sealed class PeImage
{
    private const int DosELfanewOffset = 0x3C;
    private const int NtSignatureSize = 4;

    private readonly byte[] _data;
    private readonly SectionHeader[] _sections;
    private readonly int _sizeOfHeaders;

    private PeImage(byte[] data, SectionHeader[] sections, int sizeOfHeaders, int optionalHeaderOffset, bool isPe32Plus)
    {
        _data = data;
        _sections = sections;
        _sizeOfHeaders = sizeOfHeaders;
        OptionalHeaderOffset = optionalHeaderOffset;
        IsPe32Plus = isPe32Plus;
    }

    public int OptionalHeaderOffset { get; }

    public bool IsPe32Plus { get; }

    /// <summary>数据目录起始偏移：PE32 为可选头 +96，PE32+ 为可选头 +112。</summary>
    public int DataDirectoryOffset => OptionalHeaderOffset + (IsPe32Plus ? 112 : 96);

    public static bool TryParse(byte[] data, out PeImage image)
    {
        image = null!;

        if (data.Length < DosELfanewOffset + 4 || data[0] != (byte)'M' || data[1] != (byte)'Z')
        {
            return false;
        }

        int ntOffset = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(DosELfanewOffset));
        if (ntOffset <= 0 || ntOffset + 24 > data.Length)
        {
            return false;
        }

        if (data[ntOffset] != (byte)'P' || data[ntOffset + 1] != (byte)'E' || data[ntOffset + 2] != 0 || data[ntOffset + 3] != 0)
        {
            return false;
        }

        int fileHeaderOffset = ntOffset + NtSignatureSize;
        int numberOfSections = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(fileHeaderOffset + 2));
        int sizeOfOptionalHeader = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(fileHeaderOffset + 16));
        int optionalHeaderOffset = fileHeaderOffset + 20;

        if (optionalHeaderOffset + sizeOfOptionalHeader > data.Length || optionalHeaderOffset + 2 > data.Length)
        {
            return false;
        }

        ushort magic = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(optionalHeaderOffset));
        bool isPe32Plus;
        switch (magic)
        {
            case 0x10B:
                isPe32Plus = false;
                break;
            case 0x20B:
                isPe32Plus = true;
                break;
            default:
                return false;
        }

        int sizeOfHeaders = optionalHeaderOffset + 60 + 4 <= data.Length
            ? BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(optionalHeaderOffset + 60))
            : 0;

        int sectionTableOffset = optionalHeaderOffset + sizeOfOptionalHeader;
        if (numberOfSections < 0 || sectionTableOffset + (numberOfSections * 40) > data.Length)
        {
            return false;
        }

        var sections = new SectionHeader[numberOfSections];
        for (int i = 0; i < numberOfSections; i++)
        {
            int offset = sectionTableOffset + (i * 40);
            sections[i] = new SectionHeader(
                VirtualSize: BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset + 8)),
                VirtualAddress: BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset + 12)),
                SizeOfRawData: BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset + 16)),
                PointerToRawData: BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset + 20)));
        }

        image = new PeImage(data, sections, sizeOfHeaders, optionalHeaderOffset, isPe32Plus);
        return true;
    }

    /// <summary>把 RVA 转换为文件偏移。</summary>
    public bool TryRvaToOffset(uint rva, out int offset)
    {
        offset = 0;

        if (rva < (uint)_sizeOfHeaders)
        {
            offset = (int)rva;
            return offset < _data.Length;
        }

        foreach (SectionHeader section in _sections)
        {
            int virtualAddress = section.VirtualAddress;
            int span = Math.Max(section.VirtualSize, section.SizeOfRawData);
            if (virtualAddress <= 0 || rva < (uint)virtualAddress || rva >= (uint)(virtualAddress + span))
            {
                continue;
            }

            offset = section.PointerToRawData + (int)(rva - (uint)virtualAddress);
            return offset >= 0 && offset < _data.Length;
        }

        return false;
    }

    public bool TryReadDirectoryEntry(int index, out uint rva, out uint size)
    {
        rva = 0;
        size = 0;

        int entryOffset = DataDirectoryOffset + (index * 8);
        if (entryOffset + 8 > _data.Length)
        {
            return false;
        }

        rva = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(entryOffset));
        size = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(entryOffset + 4));
        return true;
    }

    /// <summary>
    /// 读取导入表：返回 (被引用的 DLL 名, 被引用的函数名)。
    /// 与原始 <c>GetPEInformation</c> 的遍历顺序一致（先函数名后 DLL 名，用换行拼接）。
    /// </summary>
    public bool TryReadImports(out List<string> moduleNames, out List<string> functionNames)
    {
        moduleNames = [];
        functionNames = [];

        if (!TryReadDirectoryEntry(1, out uint importRva, out _) || importRva == 0)
        {
            return false;
        }

        if (!TryRvaToOffset(importRva, out int descriptorOffset))
        {
            return false;
        }

        bool found = false;

        for (int guard = 0; guard < 4096; guard++)
        {
            if (descriptorOffset + 20 > _data.Length)
            {
                break;
            }

            uint originalFirstThunk = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(descriptorOffset));
            uint nameRva = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(descriptorOffset + 12));
            uint firstThunk = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(descriptorOffset + 16));

            if (originalFirstThunk == 0 && nameRva == 0 && firstThunk == 0)
            {
                break;
            }

            // 与原始实现一致：优先用 OriginalFirstThunk，缺失时退回 FirstThunk。
            uint thunkRva = originalFirstThunk != 0 ? originalFirstThunk : firstThunk;
            if (thunkRva != 0 && TryRvaToOffset(thunkRva, out int thunkOffset))
            {
                int thunkSize = IsPe32Plus ? 8 : 4;
                int index = 0;
                while (true)
                {
                    int current = thunkOffset + (index * thunkSize);
                    if (current + thunkSize > _data.Length || index > 65535)
                    {
                        break;
                    }

                    ulong value = IsPe32Plus
                        ? BinaryPrimitives.ReadUInt64LittleEndian(_data.AsSpan(current))
                        : BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(current));

                    if (value == 0)
                    {
                        break;
                    }

                    ulong ordinalFlag = IsPe32Plus ? 0x8000_0000_0000_0000UL : 0x8000_0000UL;
                    if ((value & ordinalFlag) == 0 && TryRvaToOffset((uint)value, out int hintOffset))
                    {
                        // IMAGE_IMPORT_BY_NAME: 2 字节 hint + 以 0 结尾的函数名
                        int nameOffset = hintOffset + 2;
                        if (nameOffset < _data.Length)
                        {
                            string? name = ReadZeroTerminatedAscii(nameOffset);
                            if (!string.IsNullOrEmpty(name))
                            {
                                functionNames.Add(name);
                            }
                        }
                    }

                    index++;
                }
            }

            if (nameRva == 0 || !TryRvaToOffset(nameRva, out int moduleOffset))
            {
                break;
            }

            string? moduleName = ReadZeroTerminatedAscii(moduleOffset);
            if (!string.IsNullOrEmpty(moduleName))
            {
                moduleNames.Add(moduleName);
            }

            found = true;

            if (originalFirstThunk == 0)
            {
                break;
            }

            descriptorOffset += 20;
        }

        return found;
    }

    /// <summary>读取导出表名称列表（对应原 <c>Dll_GetExportFuncInfo</c>）。</summary>
    public IReadOnlyList<string> ReadExportNames()
    {
        var names = new List<string>();

        if (!TryReadDirectoryEntry(0, out uint exportRva, out _) || exportRva == 0)
        {
            return names;
        }

        if (!TryRvaToOffset(exportRva, out int exportOffset) || exportOffset + 40 > _data.Length)
        {
            return names;
        }

        uint numberOfNames = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(exportOffset + 24));
        uint addressOfNames = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(exportOffset + 32));

        if (numberOfNames == 0 || numberOfNames > 65536 || addressOfNames == 0)
        {
            return names;
        }

        if (!TryRvaToOffset(addressOfNames, out int namesOffset))
        {
            return names;
        }

        for (uint i = 0; i < numberOfNames; i++)
        {
            int entryOffset = namesOffset + (int)(i * 4);
            if (entryOffset + 4 > _data.Length)
            {
                break;
            }

            uint nameRva = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(entryOffset));
            if (nameRva == 0 || !TryRvaToOffset(nameRva, out int nameOffset))
            {
                continue;
            }

            string? name = ReadZeroTerminatedAscii(nameOffset);
            if (!string.IsNullOrEmpty(name))
            {
                names.Add(name);
            }
        }

        return names;
    }

    private string? ReadZeroTerminatedAscii(int offset)
    {
        int end = offset;
        while (end < _data.Length && _data[end] != 0)
        {
            end++;
        }

        if (end == offset)
        {
            return string.Empty;
        }

        return System.Text.Encoding.ASCII.GetString(_data, offset, end - offset);
    }

    private readonly record struct SectionHeader(int VirtualSize, int VirtualAddress, int SizeOfRawData, int PointerToRawData);
}
