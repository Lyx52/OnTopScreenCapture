namespace OnTopCapture.Utils.Enums
{
    /// <summary>
    /// Win32 ancestor window flags
    /// </summary>
    internal enum GetAncestorFlags : int
    {
        // Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
        GetParent = 1,
        // Retrieves the root window by walking the chain of parent windows.
        GetRoot = 2,
        // Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
        GetRootOwner = 3
    }
}
