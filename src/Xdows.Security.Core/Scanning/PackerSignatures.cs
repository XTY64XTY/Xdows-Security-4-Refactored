namespace Xdows.Security.Core.Scanning;

/// <summary>
/// 壳检测规则，移植自原引擎的 <c>启发_检测壳子</c> / <c>启发_高级检测壳子</c>。
/// 规则顺序与原始代码保持一致，命中任一条即视为被加壳。
/// </summary>
internal static class PackerSignatures
{
    /// <summary>对应 <c>启发_检测壳子</c>：返回壳名，未命中返回 <c>"-1"</c>。</summary>
    public static string DetectNamedPacker(ReadOnlySpan<byte> content)
    {
        if (ByteSearch.ContainsText(content, "Pantikill"))
        {
            return "Pantikill";
        }

        if (ByteSearch.ContainsText(content, "P.vmp0"))
        {
            return "VMP";
        }

        if (ByteSearch.ContainsText(content, "PECompact"))
        {
            return "PECompact";
        }

        if (ByteSearch.ContainsText(content, "ex_cod"))
        {
            return "eXPressor";
        }

        if (ByteSearch.ContainsText(content, "FSG!"))
        {
            return "FSG";
        }

        if (ByteSearch.ContainsText(content, "MaskPE"))
        {
            return "MaskPE";
        }

        return "-1";
    }

    /// <summary>对应 <c>启发_高级检测壳子</c>，包含末尾针对前 448 字节的启发式判断。</summary>
    public static bool DetectAdvancedPacker(byte[] content)
    {
        bool result = false;
        bool C(string value) => ByteSearch.ContainsText(content, value);

        if (C("d.dllB") && C("UTDFGHKHCOOLWW"))
        {
            result = true;
        }

        if (C("蟭Gf蟭df冿dum") && C("隚f佺f蟭"))
        {
            result = true;
        }

        if (C("鄗wc"))
        {
            result = true;
        }

        if (C("ysdm0"))
        {
            result = true;
        }

        if (C("纒dt") && C("U嬱侅"))
        {
            result = true;
        }

        if (C("珣c蠮") && C("妹埶Й"))
        {
            result = true;
        }

        if (C("艒?硼6壛Bo"))
        {
            result = true;
        }

        if (C("y") && (C("yoda") || C("uying")))
        {
            result = true;
        }

        if (C("P") || C("p"))
        {
            if (C("P.vmp0") || C("Pantikill") || C(".petprg") || C("Pkaixin") || C("Packer") || C("P.Silvana") ||
                C("PECompact") || C("packerB"))
            {
                result = true;
            }

            if (C("pde") && C("AStub"))
            {
                result = true;
            }

            if (C("pelock") && C("CODE"))
            {
                result = true;
            }

            if (C("perplex") || C("PEPACK") || C("Petite") || C("Protected By 007"))
            {
                result = true;
            }

            if (C("pec1") && C("PECO"))
            {
                result = true;
            }

            if (C("packerBY!Y"))
            {
                result = true;
            }
        }

        if (C("s") || C("S"))
        {
            if (C("SDP!") || C("SDPC") || C("svkp"))
            {
                result = true;
            }
        }

        if (C("RPCrypt"))
        {
            result = true;
        }

        if (C("Richny"))
        {
            result = true;
        }

        if (C("ll") && (C("llydd") || C("lhhack")))
        {
            result = true;
        }

        if (C("LUHW"))
        {
            result = true;
        }

        if (C("T"))
        {
            if (C("TopFox") || C("Themida"))
            {
                result = true;
            }

            if (C("TObject%爯") && C("嬂%垚"))
            {
                result = true;
            }
        }

        if (C("MZ Run Me!"))
        {
            result = true;
        }

        if (HeaderEquals(content, "MZKERNEL32.DLL"))
        {
            result = true;
        }

        if (HeaderEquals(content, "MZLoadLibraryA"))
        {
            result = true;
        }

        if (C("o") && (C("oda & M.o.D") || C("kceotk") || C("okpack")))
        {
            result = true;
        }

        if (C("m") && C("vmp0"))
        {
            result = true;
        }

        if (C("节肄鱷嵻飐"))
        {
            result = true;
        }

        if (C("j") && ((C("jectd3") && C("IIn")) || (C("jdpack") && C("CODE")) || (C("jbxp") && C("data"))))
        {
            result = true;
        }

        if (C("DAStub"))
        {
            result = true;
        }

        if (C("AStub") && C("HH"))
        {
            result = true;
        }

        if (C("鸱s绔f蕝8縫#糷"))
        {
            result = true;
        }

        if (C("妹X~敖g_莐讧d=薜/"))
        {
            result = true;
        }

        if (C(" 盗  亦  有  道"))
        {
            result = true;
        }

        if (C("xntdqo魎czf黺"))
        {
            result = true;
        }

        if (C(".BEAR") && C(".data"))
        {
            result = true;
        }

        if (C("FSG!"))
        {
            result = true;
        }

        if (C("K") && (C("Kaos2") || C("KnPack") || C("WGHKLDS")))
        {
            result = true;
        }

        if (C("n") && (C("naked1") || C("nybu")))
        {
            result = true;
        }

        if (C("N\twB[s"))
        {
            result = true;
        }

        if (C("Z0L1S鏇誡_E塕"))
        {
            result = true;
        }

        if (C("MaskPE"))
        {
            result = true;
        }

        if (C("i") && (C("ByDwing") || C("inKripT v1.0 by MrCrimso") || C("imerboxu")))
        {
            result = true;
        }

        if (C("Upack") && C("疤輚xne鴎衧3e馎"))
        {
            result = true;
        }

        if (C("f") && C("AN 昢7Rf丠\t"))
        {
            result = true;
        }

        if (C("F") && ((C("FilMsg.e") && C("d.dll")) || C("Fuck_Rav")))
        {
            result = true;
        }

        if (C("h") && C("hmimys's Protect"))
        {
            result = true;
        }

        if (C("nsp0") && !C("ecode"))
        {
            result = true;
        }

        if (C("浧UH"))
        {
            result = true;
        }

        if (C("襏寊"))
        {
            result = true;
        }

        if (C("bbmmllUUUUU"))
        {
            result = true;
        }

        if (C("jhh1G") && C("8h8BI"))
        {
            result = true;
        }

        if (C("SMTP邮件"))
        {
            result = true;
        }

        if (C("M60IM70"))
        {
            result = true;
        }

        if (C("529376"))
        {
            result = true;
        }

        if (C("nsp1") && C("nsp2"))
        {
            result = true;
        }

        if (C("UPX"))
        {
            result = true;
        }

        if (C("UPX0") && C("UPX1"))
        {
            result = true;
        }

        if (C("idata") && C("PAGELK") && C("k31"))
        {
            result = true;
        }

        if (C("_PAGELK"))
        {
            result = true;
        }

        if (C("P.vmp0") && C("P.vmp1"))
        {
            result = true;
        }

        if (C("lrslr") && C("idata"))
        {
            result = true;
        }

        if (C("Psugite"))
        {
            result = true;
        }

        if (C("myow0") && C("myow1"))
        {
            result = true;
        }

        if (C("FSG!"))
        {
            result = true;
        }

        if (C("deuhhz0") && C("deuhhz1") && C("deuhhz2"))
        {
            result = true;
        }

        if (C("MaskPE"))
        {
            result = true;
        }

        if (C("PRESS ANY KEY TO EXIT"))
        {
            result = true;
        }

        if (C("P.packed0") && C("P.packed1"))
        {
            result = true;
        }

        if (C("zzzz21"))
        {
            result = true;
        }

        if (C("jInt4"))
        {
            result = true;
        }

        if (C("dking"))
        {
            result = true;
        }

        if (C("Upack"))
        {
            result = true;
        }

        if (C("myex0") && C("myex1") && C("myex2"))
        {
            result = true;
        }

        if (C("zutwl0") && C("zutwl1") && C("zutwl2"))
        {
            result = true;
        }

        if (C("Ows3Tw"))
        {
            result = true;
        }

        if (C("weIJUNLi"))
        {
            result = true;
        }

        if (C("ex_cod"))
        {
            result = true;
        }

        if (C("pediy"))
        {
            result = true;
        }

        if (C("p") && C("H鸃"))
        {
            result = true;
        }

        if (C("P") && C("packed0") && C("packed1"))
        {
            result = true;
        }

        if (C("vmp0") && C("vmp1"))
        {
            result = true;
        }

        if (DetectSectionNameHeuristic(content))
        {
            result = true;
        }

        return result;
    }

    /// <summary>对应 <c>到文本 (取字节集左边 (文件数据, 14)) = 内容</c>。</summary>
    private static bool HeaderEquals(byte[] content, string value)
    {
        int length = Math.Min(14, content.Length);
        return Text.AnsiText.ToText(content.AsSpan(0, length)) == value;
    }

    /// <summary>
    /// 原引擎末尾的段名启发式：取前 448 字节，若含 0x00/0x01/0x02，
    /// 则取出偏移 311 起的 10 字节、去掉 0x00 与字符 0 后回查 "…1"，段名不是 upx 即判定为壳。
    /// </summary>
    private static bool DetectSectionNameHeuristic(byte[] content)
    {
        ReadOnlySpan<byte> head = content.Length > 448 ? content.AsSpan(0, 448) : content;
        if (head.Length < 321)
        {
            return false;
        }

        if (head.IndexOf((byte)0) < 0 || head.IndexOf((byte)1) < 0 || head.IndexOf((byte)2) < 0)
        {
            return false;
        }

        byte[] slice = head.Slice(310, 10).ToArray();
        var filtered = new List<byte>(slice.Length);
        foreach (byte value in slice)
        {
            if (value == 0 || value == (byte)' ')
            {
                continue;
            }

            filtered.Add(value);
        }

        var withoutZeroChar = new List<byte>(filtered.Count);
        foreach (byte value in filtered)
        {
            if (value == (byte)'0')
            {
                continue;
            }

            withoutZeroChar.Add(value);
        }

        byte[] candidate = [.. withoutZeroChar];
        byte[] probe = [.. candidate, (byte)'1'];
        if (head.IndexOf(probe) < 0)
        {
            return false;
        }

        string text = RemoveAllWhitespace(Text.AnsiText.ToText(candidate));
        if (text.Length == 0)
        {
            return false;
        }

        return !string.Equals(text, "upx", StringComparison.Ordinal);
    }

    /// <summary>对应易语言的 <c>删全部空</c>。</summary>
    private static string RemoveAllWhitespace(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (char ch in value)
        {
            if (ch is ' ' or '\t' or '\r' or '\n' or '\u3000')
            {
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }
}
