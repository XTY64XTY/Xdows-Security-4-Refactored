using System.Runtime.InteropServices;

namespace Xdows.Security.Host.Interaction;

/// <summary>通用文件对话框（IFileOpenDialog）互操作声明。</summary>
internal static class NativeFileDialog
{
    private const uint FosPickFolders = 0x00000020;
    private const uint FosForceFileSystem = 0x00000040;
    private const uint FosPathMustExist = 0x00000800;
    private const uint FosFileMustExist = 0x00001000;
    private const uint FosNoChangeDir = 0x00000008;

    private const int ErrorCancelled = unchecked((int)0x800704C7);
    private const uint SigdnFileSysPath = 0x80058000;

    public static string? OpenFile(IntPtr owner, string title, string initialDirectory, string filter)
    {
        IFileDialog? dialog = null;
        try
        {
            dialog = (IFileDialog)new FileOpenDialogComObject();
            dialog.GetOptions(out uint options);
            dialog.SetOptions(options | FosForceFileSystem | FosPathMustExist | FosFileMustExist | FosNoChangeDir);

            if (title.Length > 0)
            {
                dialog.SetTitle(title);
            }

            ApplyFilter(dialog, filter);
            ApplyInitialFolder(dialog, initialDirectory);

            int hr = dialog.Show(owner);
            if (hr == ErrorCancelled)
            {
                return null;
            }

            Marshal.ThrowExceptionForHR(hr);

            dialog.GetResult(out IShellItem item);
            return GetPath(item);
        }
        catch (COMException)
        {
            return null;
        }
        finally
        {
            if (dialog != null)
            {
                Marshal.ReleaseComObject(dialog);
            }
        }
    }

    public static string? OpenFolder(IntPtr owner, string title)
    {
        IFileDialog? dialog = null;
        try
        {
            dialog = (IFileDialog)new FileOpenDialogComObject();
            dialog.GetOptions(out uint options);
            dialog.SetOptions(options | FosPickFolders | FosForceFileSystem | FosPathMustExist | FosNoChangeDir);

            if (title.Length > 0)
            {
                dialog.SetTitle(title);
            }

            int hr = dialog.Show(owner);
            if (hr == ErrorCancelled)
            {
                return null;
            }

            Marshal.ThrowExceptionForHR(hr);

            dialog.GetResult(out IShellItem item);
            return GetPath(item);
        }
        catch (COMException)
        {
            return null;
        }
        finally
        {
            if (dialog != null)
            {
                Marshal.ReleaseComObject(dialog);
            }
        }
    }

    /// <summary>把 “名称 (*.zip)|*.zip|所有文件 (*.*)|*.*” 形式的过滤器传给对话框。</summary>
    private static void ApplyFilter(IFileDialog dialog, string filter)
    {
        string[] parts = filter.Split('|');
        if (parts.Length < 2)
        {
            return;
        }

        int count = parts.Length / 2;
        var specs = new COMDLG_FILTERSPEC[count];
        for (int i = 0; i < count; i++)
        {
            specs[i] = new COMDLG_FILTERSPEC
            {
                pszName = parts[i * 2],
                pszSpec = parts[(i * 2) + 1],
            };
        }

        dialog.SetFileTypes((uint)count, specs);
        dialog.SetFileTypeIndex(1);
    }

    private static void ApplyInitialFolder(IFileDialog dialog, string initialDirectory)
    {
        if (initialDirectory.Length == 0 || !Directory.Exists(initialDirectory))
        {
            return;
        }

        Guid shellItemGuid = typeof(IShellItem).GUID;
        int hr = SHCreateItemFromParsingName(initialDirectory, IntPtr.Zero, ref shellItemGuid, out IShellItem folder);
        if (hr != 0 || folder == null)
        {
            return;
        }

        try
        {
            dialog.SetFolder(folder);
        }
        finally
        {
            Marshal.ReleaseComObject(folder);
        }
    }

    private static string? GetPath(IShellItem item)
    {
        if (item == null)
        {
            return null;
        }

        IntPtr buffer = IntPtr.Zero;
        try
        {
            item.GetDisplayName(SigdnFileSysPath, out buffer);
            return buffer == IntPtr.Zero ? null : Marshal.PtrToStringUni(buffer);
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(buffer);
            }

            Marshal.ReleaseComObject(item);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int SHCreateItemFromParsingName(
        string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct COMDLG_FILTERSPEC
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszSpec;
    }

    [ComImport]
    [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
    [ClassInterface(ClassInterfaceType.None)]
    private class FileOpenDialogComObject
    {
    }

    [ComImport]
    [Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileDialog
    {
        // IModalWindow
        [PreserveSig]
        int Show(IntPtr parent);

        void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);

        void SetFileTypeIndex(uint iFileType);

        void GetFileTypeIndex(out uint piFileType);

        void Advise(IntPtr pfde, out uint pdwCookie);

        void Unadvise(uint dwCookie);

        void SetOptions(uint fos);

        void GetOptions(out uint pfos);

        void SetDefaultFolder(IShellItem psi);

        void SetFolder(IShellItem psi);

        void GetFolder(out IShellItem ppsi);

        void GetCurrentSelection(out IShellItem ppsi);

        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        void GetFileName(out IntPtr pszName);

        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);

        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        void GetResult(out IShellItem ppsi);

        void AddPlace(IShellItem psi, int fdap);

        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

        void Close(int hr);

        void SetClientGuid(ref Guid guid);

        void ClearClientData();

        void SetFilter(IntPtr pFilter);
    }

    [ComImport]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

        void GetParent(out IShellItem ppsi);

        void GetDisplayName(uint sigdnName, out IntPtr ppszName);

        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

        void Compare(IShellItem psi, uint hint, out int piOrder);
    }
}
