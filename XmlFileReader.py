import clr

clr.AddReference("System.Core")
clr.AddReference('System.IO')
clr.AddReference('System.Xml')
clr.AddReference("System.Xml.Linq")
clr.AddReference(
    "MES.MQC.DataSourceLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=4d95002c27fab778"
)

import System

clr.ImportExtensions(System.Linq)
clr.ImportExtensions(System.Xml.XPath)

from System import String, Double, DateTime
from System.Collections.Generic import List
from MES.MQC.DataSourceLibrary.Adapters import Adapter
from MES.MQC.DataSourceLibrary.Models.Adapters import AdapterData, AdapterReadResult

class XmlFileReader(FileReader):
    """
    A adapter has to extend from the Adapter class which is the base for all MQC Adapters
    The ClassName should contain the Format of the Adapter (e.g Xml) and the "FileReader" keyword at the end (e.g ExampleXmlFileReader where Example is the name)
    """

    def get_Name(self):
        """
        Unique Name of the Adapter.
        """
        return "Python Example"

    def get_Description(self):
        """
        Description of the Adapter that is visible in the Adapter-Dialog as a Popover.
        Absolute links get transformed into HTML Link-Tags,
        Linebreaks (\n) get transformed into HTML linebreaks (<![CDATA[<br>]]>),
        HTML Tags are not allowed.
        """
        return "This is an example adapter"

    def get_Priority(self):
        """
        Priority of the Adapter.
        The execution order of all adapters depend on the defined priorities.
        The higher the priority the earlier the adapter is validated and executed.
        The default priority for Base Adapters is between 10-110, Custom Adapters should define a priority
        of 200 or higher if they should be executed before the base adapters.
        If two adapters have the same priority, the order is depending on the Name of the adapter.
        """
        return 200

    def get_FileExtensions(self):
        """
        File Extensions of the Adapter.
        This Property has to be defined and has to have at least one file extension.
        The Adapter is only used for FilePaths with the defined file extensions.
        The IsValid method is not called unless the file extension matches.
       """
        return List[String]([".xml"])

    def get_DataSource(self):
        """
        The Data Source of the Adapter.
        If a report file contains data from multiple data sources, this property has to be to "Unknown" and
        the DataSource of each AdapterData object has to be defined.
        """
        return "Example"

    def IsValid(self, context):
        """
        IsValid has to be implemented by the Adapter class.
        The method is called when the file extensions match.
        If the file extension is unique this method can just return true, else it should check if the file
        should be imported by the adapter.
        If true is returned, the current adapter is executed and the Read method is called.
        No other adapter is checked afterward.

        Parameters
        ----------
        context : FileReaderContext
        """
        return context.Name == "Report.Example.xml"

    def Read(self, context):
        """
        Read has to be implemented by the Adapter class.
        This method is called when the file extensions match and isValid returns true, no other adapter is called.
        The data of the file should be read and returned as a AdapterReadResult.

        Parameters
        ----------
        context : FileReaderContext
        """
        document = context.AsXDocument()
        result = AdapterReadResult()

        return result
