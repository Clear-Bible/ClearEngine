using System;

using ClearBible.Clear3.API;
using ClearBible.Clear3.APIImportExport;

namespace ClearBible.Clear3.ServiceImportExport
{
    public class Clear30ServiceImportExport
    {
        public static IClear30ServiceAPIImportExport Create() =>
            new Clear30ServiceAPIImportExport();
    }


    internal class Clear30ServiceAPIImportExport
        : IClear30ServiceAPIImportExport
    {

    }
}
