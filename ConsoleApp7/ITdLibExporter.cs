using System.Collections.Generic;

namespace ConsoleApp7
{
    public interface ISmalltalkTypeExporter
    {
        IEnumerable<SmalltalkClass> GetExportedObjects();
    }
}