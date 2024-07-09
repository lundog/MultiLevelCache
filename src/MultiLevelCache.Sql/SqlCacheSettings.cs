using System.Data;
using System;

namespace MultiLevelCaching.Sql
{
    public class SqlCacheSettings
    {
        public Func<IDbConnection> DbFactory { get; set; }

        public string Schema {  get; set; }

        public string Table { get; set; }

        public string TableType { get; set; }
    }
}
