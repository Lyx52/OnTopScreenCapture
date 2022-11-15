using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static OnTopCapture.Utils.ExternalApi;
namespace OnTopCapture.Utils
{

    internal static class ProcessExtensions
    {
        /// <summary>
        /// Get the main module name/executable name
        /// </summary>
        /// <param name="process">Process ModuleName belongs to</param>
        /// <param name="buffer">Size of buffer to allocate default is 1024</param>
        /// <returns></returns>
        public static string GetMainModuleFileName(this Process process, int buffer = 1024)
        {
            var fileNameBuilder = new StringBuilder(buffer);
            uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
            return QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength) != 0 ?
                fileNameBuilder.ToString() :
                null;
        }
        /// <summary>
        /// Extract icon from a process
        /// </summary>
        /// <param name="process">Process icon belongs to</param>
        /// <returns></returns>
        public static Icon GetIcon(this Process process)
        {
            try
            {
                string mainModuleFileName = process.GetMainModuleFileName();
                return Icon.ExtractAssociatedIcon(mainModuleFileName);
            }
            catch
            {
                return null;
            }
        }
    }
}
