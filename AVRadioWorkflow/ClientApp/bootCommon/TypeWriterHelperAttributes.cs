/*
 * Contains attributes that effects how typewriter export classes
*/
using System;


namespace bootCommon
{
    /// <summary>
    /// gets model.tst to include an import
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ForceTypeImportAttribute : Attribute
    {
        public string Value { get; private set; }
        public ForceTypeImportAttribute(string value)
        {
            this.Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ExportAsOptionalAttribute : Attribute
    {
        
    }
}
