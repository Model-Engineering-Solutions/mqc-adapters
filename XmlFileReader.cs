using System;
using System.Collections.Generic;
using MES.MQC.DataSourceLibrary.Adapters;
using MES.MQC.DataSourceLibrary.Context;
using MES.MQC.DataSourceLibrary.Models.Adapters;

// a namespace is optional and the naming is up to the developer
namespace MES.MQC.CustomAdapters
{
    /// <summary>
    ///    A adapter has to extend from the Adapter class which is the base for all MQC Adapters.
    ///    The ClassName should contain the Format of the Adapter (e.g Xml) and the "FileReader"
    ///    keyword at the end (e.g ExampleXmlFileReader where "Example" is the name)
    /// </summary>
    public class XmlFileReader : FileReader
    {
        /// <summary>
        ///   Unique Name of the Adapter.
        /// </summary>
        public override string Name => "Example";

        /// <summary>
        ///   Description of the Adapter that is visible in the Adapter Dialog as a popover.
        ///   Absolute links get transformed into HTML Link-Tags,
        ///   Line breaks (\n) get transformed into HTML line breaks (<![CDATA[<br>]]>),
        ///   HTML tags are not allowed.
        /// </summary>
        public override string Description => "This is an example adapter";

        public override Version Version => new Version(8, 3, 0);

        /// <summary>
        ///   Priority of the Adapter.
        ///   The execution order of all adapters depend on the defined priorities.
        ///   The higher the priority the earlier the adapter is validated and executed.
        ///   The default priority for Base Adapters is between 10-110, Custom Adapters should define a priority
        ///   of 200 or higher if they should be executed before the base adapters.
        ///   If two adapters have the same priority, the order is depending on the name of the adapter.
        /// </summary>
        public override int Priority => 200;

        /// <summary>
        ///   File Extensions of the Adapter.
        ///   This property has to be defined and must have at least one file extension.
        ///   The Adapter is only used for FilePaths with the defined file extensions.
        ///   The IsValid method is not called unless the file extension matches.
        /// </summary>
        public override List<string> FileExtensions => new List<string> { ".xml" };

        /// <summary>
        ///   The Data Source of the Adapter.
        ///   If a report file contains data from multiple data sources, this property has to be to "Unknown" and
        ///   the DataSource of each AdapterData object has to be defined.
        /// </summary>
        public override string DataSource => "Example";

        /// <summary>
        ///   IsValid has to be implemented by the Adapter class.
        ///   The method is called when the file extensions match.
        ///   If the file extension is unique, this method can just return true, otherwise it should check if the file
        ///   should be imported by the adapter.
        ///   If true is returned, the current adapter is executed and the Read method is called.
        ///   No other adapter is checked afterward.
        /// </summary>
        /// <param name="context">FileReaderContext</param>
        /// <returns>boolean (is this the correct adapter to read the file?)</returns>
        protected override bool IsValid(FileReaderContext context)
        {
            return context.FileName == "Report.Example.xml";
        }

        /// <summary>
        ///   Read has to be implemented by the Adapter class.
        ///   This method is called when the file extensions match and isValid returns true, no other adapter is called.
        ///   The data of the file should be read and returned as a AdapterReadResult.
        /// </summary>
        /// <param name="context">FileReaderContext</param>
        /// <returns>AdapterReadResult</returns>
        protected override AdapterReadResult Read(FileReaderContext context)
        {
            var document = context.AsXDocument();
            var result = new AdapterReadResult();

            return result;
        }

        public class ExampleXmlFileReaderOptions : FileReaderAdapterOptions
        {
        }
    }
}