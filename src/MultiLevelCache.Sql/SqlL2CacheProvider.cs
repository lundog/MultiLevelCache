using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MultiLevelCaching.Sql
{
    public class SqlL2CacheProvider : IL2CacheProvider
    {
        protected string Schema { get; }
        protected string Table { get; }
        protected string TableType { get; }

        private const string DefaultSchema = "dbo";
        private const string DefaultTable = "CacheItems";
        private const string DefaultTableType = "CacheItemsTable";

        private readonly Func<IDbConnection> _dbFactory;

        public SqlL2CacheProvider(
            SqlCacheSettings settings)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _dbFactory = settings.DbFactory ?? throw new ArgumentException($"{nameof(settings.DbFactory)} is required.", nameof(settings.DbFactory));
            Schema = string.IsNullOrEmpty(settings.Schema) ? DefaultSchema : settings.Schema;
            Table = string.IsNullOrEmpty(settings.Table) ? DefaultTable : settings.Table;
            TableType = string.IsNullOrEmpty(settings.TableType) ? DefaultTableType : settings.TableType;
        }

        public async Task<byte[]> Get(string key)
        {
            using (var db = Db())
            {
                return await db.QueryFirstOrDefaultAsync<byte[]>($@"
SELECT
	[Value]
FROM [{Schema}].[{Table}]
WHERE [Key] = @Key
	AND [Expiration] > GETUTCDATE()
",
                    new { Key = key }
                ).ConfigureAwait(false);
            }
        }

        public async Task<IList<byte[]>> Get(IEnumerable<string> keys)
        {
            var keysList = keys.AsList();
            var rows = keysList.Select(key => new CacheItemsDataRow
            {
                Key = key
            });

            using (var db = Db())
            using (var itemsTable = ToCacheItemsTable(rows))
            {
                var values = (await db.QueryAsync<CacheItemsDataRow>($@"
SELECT
	C.[Key],
	C.[Value]
FROM [{Schema}].[{Table}] C
    JOIN @Items I ON C.[Key] = I.[Key]
WHERE C.[Expiration] > GETUTCDATE()
",
                    new { Items = ToCacheItemsTableParameter(itemsTable) }
                ).ConfigureAwait(false)).ToDictionary(e => e.Key, e => e.Value);

                return keysList
                    .Select(key => values.TryGetValue(key, out var value) ? value : null)
                    .ToList();
            }
        }

        public async Task Remove(string key)
        {
            using (var db = Db())
            {
                await db.ExecuteAsync(
                    $"DELETE [{Schema}].[{Table}] WHERE [Key] = @Key",
                    new { Key = key }
                ).ConfigureAwait(false);
            }
        }

        public async Task Remove(IEnumerable<string> keys)
        {
            var rows = keys.Select(key => new CacheItemsDataRow
            {
                Key = key
            });

            using (var db = Db())
            using (var itemsTable = ToCacheItemsTable(rows))
            {
                await db.ExecuteAsync($@"
DELETE C
FROM [{Schema}].[{Table}] C
    JOIN @Items I ON C.[Key] = I.[Key]
",
                    new { Items = ToCacheItemsTableParameter(itemsTable) }
                ).ConfigureAwait(false);
            }
        }

        public async Task Set(string key, byte[] value, TimeSpan duration)
        {
            var expiration = ToExpiration(duration);

            using (var db = Db())
            {
                await db.ExecuteAsync($@"
MERGE [{Schema}].[{Table}] WITH (HOLDLOCK) T
USING (VALUES (@Key, @Value, @Expiration)) S ([Key], [Value], [Expiration])
	ON T.[Key] = S.[Key]
WHEN MATCHED THEN
	UPDATE SET [Value] = S.[Value], [Expiration] = S.[Expiration]
WHEN NOT MATCHED THEN
	INSERT ([Key], [Value], [Expiration]) VALUES (S.[Key], S.[Value], S.[Expiration]);
",
                    new { Key = key, Value = value, Expiration = expiration }
                ).ConfigureAwait(false);
            }
        }

        public async Task Set(IEnumerable<KeyValuePair<string, byte[]>> values, TimeSpan duration)
        {
            var expiration = ToExpiration(duration);
            var rows = values.Select(valuePair => new CacheItemsDataRow
            {
                Key = valuePair.Key,
                Value = valuePair.Value,
                Expiration = expiration
            });

            using (var db = Db())
            using (var itemsTable = ToCacheItemsTable(rows))
            {
                await db.ExecuteAsync($@"
UPDATE C SET
	[Value] = I.[Value],
	[Expiration] = I.[Expiration]
FROM [{Schema}].[{Table}] C
    JOIN @Items I ON C.[Key] = I.[Key]
",
                    new { Items = ToCacheItemsTableParameter(itemsTable) }
                ).ConfigureAwait(false);
            }
        }

        private IDbConnection Db()
            => _dbFactory();

        private static DataTable ToCacheItemsTable(IEnumerable<CacheItemsDataRow> rows)
        {
            var table = new DataTable();

            table.Columns.Add(new DataColumn("Key", typeof(string)) { AllowDBNull = false });
            table.Columns.Add(new DataColumn("Value", typeof(byte[])));
            table.Columns.Add(new DataColumn("Expiration", typeof(DateTime)));
            
            foreach (var row in rows)
            {
                table.Rows.Add(row.Key, row.Value, row.Expiration);
            }

            return table;
        }

        private SqlMapper.ICustomQueryParameter ToCacheItemsTableParameter(DataTable table)
            => table.AsTableValuedParameter($"[{Schema}].[{TableType}]");

        private static DateTime ToExpiration(TimeSpan duration)
            => DateTime.UtcNow.Add(duration);

        private class CacheItemsDataRow
        {
            public string Key { get; set; }
            public byte[] Value { get; set; }
            public DateTime? Expiration { get; set; }
        }
    }
}
