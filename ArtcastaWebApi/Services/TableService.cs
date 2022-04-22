using ArtcastaWebApi.Helpers;
using ArtcastaWebApi.Models;
using System.Data;
using System.Data.SqlClient;

namespace ArtcastaWebApi.Services
{
    public class TableService : ITableService
    {
        private readonly IConfiguration _config;
        private readonly string sqlDataSource;

        public TableService(IConfiguration config)
        {
            _config = config;
            sqlDataSource = _config.GetConnectionString("ArtcastaAppCon");
        }
        public List<TableAttribute> GetTableAttributes(int tableId)
        {
            string queryAttr = "select AttrId, TableId, SystemAttrName, AttrName, PkFlag, a.AttrTypeId, AttrTypeProp1, AttrTypeProp2, Ord " +
                               "from dbo.Attributes a inner join dbo.AttrTypes t on a.AttrTypeId = t.AttrTypeId where TableId = @tableId order by Ord;";
            List<TableAttribute> tableAttributes = new List<TableAttribute>();


            using (SqlConnection attrCon = new SqlConnection(sqlDataSource))
            {
                attrCon.Open();
                using (SqlCommand attrCommand = new SqlCommand(queryAttr, attrCon))
                {
                    attrCommand.Parameters.AddWithValue("@tableId", tableId);
                    SqlDataReader attrReader = attrCommand.ExecuteReader();
                    while (attrReader.Read())
                    {
                        TableAttribute attr = new TableAttribute
                        {
                            AttrId = attrReader.GetInt32(attrReader.GetOrdinal("AttrId")),
                            TableId = attrReader.GetInt32(attrReader.GetOrdinal("TableId")),
                            SystemAttrName = attrReader.GetString(attrReader.GetOrdinal("SystemAttrName")),
                            AttrName = attrReader.GetString(attrReader.GetOrdinal("AttrName")),
                            PkFlag = attrReader.GetInt32(attrReader.GetOrdinal("PkFlag")),
                            AttrTypeId = attrReader.GetInt32(attrReader.GetOrdinal("AttrTypeId")),
                            AttrTypeProp1 = SqlReader.GetNullableInt(attrReader, attrReader.GetOrdinal("AttrTypeProp1")),
                            AttrTypeProp2 = SqlReader.GetNullableInt(attrReader, attrReader.GetOrdinal("AttrTypeProp2")),
                            Ord = attrReader.GetInt32(attrReader.GetOrdinal("Ord"))
                        };
                        tableAttributes.Add(attr);
                    }
                    attrReader.Close();
                }
                attrCon.Close();
            }

            return tableAttributes;
        }
        private DataTable GetTableData(string tableName, List<TableAttribute> attributes)
        {
            string queryData = @"select ";
            string joinStr = "";
            List<AttributeType> attrTypes = GetAttributeTypes();
            List<Table> tablesLoaded = new List<Table>();
            foreach (TableAttribute attr in attributes)
            {
                if (attr.AttrTypeId == attrTypes.Find(attr => attr.SystemAttrTypeName == "join")?.AttrTypeId)
                {
                    if (!attr.AttrTypeProp1.HasValue)
                    {
                        throw new Exception("не указана таблица присоединения");
                    }
                    if (!attr.AttrTypeProp2.HasValue)
                    {
                        throw new Exception("не выбрано поле у таблицы присоединения");
                    }

                    Table joinTable;
                    if (tablesLoaded.Any(t => t.TableId == attr.AttrTypeProp1.Value))
                    {
                        joinTable = tablesLoaded.Find(t => t.TableId == attr.AttrTypeProp1.Value);
                    }
                    else
                    {
                        joinTable = GetTableWithAttributes(attr.AttrTypeProp1.Value);
                        tablesLoaded.Add(joinTable);
                    }
                    if (joinTable == null)
                    {
                        throw new Exception("таблица присоединения не найдена");
                    }
                    TableAttribute joinAttr = joinTable.Attributes.Find(a => a.PkFlag == 1);
                    if (joinAttr == null)
                    {
                        throw new Exception("первичный ключ у таблицы присоединения не найден");
                    }
                    TableAttribute selectAttr = joinTable.Attributes.Find(a => a.AttrId == attr.AttrTypeProp2.Value);
                    if (selectAttr == null)
                    {
                        throw new Exception("выбранное поле у таблицы присоединения не найдено");
                    }
                    //joinStr += $" inner join {joinTable.SystemTableName} j on t.{attr.SystemAttrName} = j.{joinAttr.SystemAttrName}";
                    //queryData += $"j.{selectAttr.SystemAttrName} as {attr.SystemAttrName}, ";
                }
                queryData += "t." + attr.SystemAttrName + ", ";
            }
            queryData = queryData.Remove(queryData.Length - 2);


            queryData += @" from dbo." + tableName + @" t";

            if (joinStr.Length > 0)
            {
                queryData += joinStr;
            }

            DataTable dt = new DataTable();
            using (SqlConnection dataCon = new SqlConnection(sqlDataSource))
            {
                dataCon.Open();
                using (SqlCommand dataCommand = new SqlCommand(queryData, dataCon))
                {
                    SqlDataReader dataReader = dataCommand.ExecuteReader();
                    dt.Load(dataReader);
                    dataReader.Close();
                }
                dataCon.Close();
            }


            return dt;
        }
        public List<Table> GetTables()
        {
            string queryTable = "select TableId, SystemTableName, TableName, CategoryId, Ord from dbo.UserTables order by Ord;";

            List<Table> tables = new List<Table>();
            SqlDataReader myReader;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(queryTable, myConn))
                {
                    myReader = myCommand.ExecuteReader();
                    while (myReader.Read())
                    {
                        Table table = new Table
                        {
                            TableId = myReader.GetInt32(myReader.GetOrdinal("TableId")),
                            SystemTableName = myReader.GetString(myReader.GetOrdinal("SystemTableName")),
                            TableName = myReader.GetString(myReader.GetOrdinal("TableName")),
                            CategoryId = myReader.GetInt32(myReader.GetOrdinal("CategoryId")),
                            Ord = myReader.GetInt32(myReader.GetOrdinal("Ord"))
                        };

                        table.Attributes = GetTableAttributes(table.TableId);
                        table.Data = GetTableData(table.SystemTableName, table.Attributes);

                        tables.Add(table);

                    }

                    myReader.Close();
                }
                myConn.Close();
            }
            return tables;
        }
        public Table GetTableInfo(int tableId)
        {
            string queryTable = @"select top 1 TableId, SystemTableName, TableName, CategoryId, Ord from dbo.UserTables where TableId = @tableId;";

            SqlDataReader myReader;
            Table table = new Table();
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(queryTable, myConn))
                {
                    myCommand.Parameters.AddWithValue("@tableId", tableId);
                    myReader = myCommand.ExecuteReader();
                    while (myReader.Read())
                    {
                        table = new Table
                        {
                            TableId = (int)myReader["TableId"],
                            SystemTableName = myReader["SystemTableName"].ToString(),
                            TableName = myReader["TableName"].ToString(),
                            CategoryId = (int)myReader["CategoryId"],
                            Ord = (int)myReader["Ord"]
                        };

                    }

                    myReader.Close();
                }
                myConn.Close();
            }
            return table;
        }
        public Table GetTable(int tableId)
        {
            Table table = GetTableInfo(tableId);
            table.Attributes = GetTableAttributes(tableId);
            table.Data = GetTableData(table.SystemTableName, table.Attributes);

            return table;
        }
        private Table GetTableWithAttributes(int tableId)
        {
            Table table = GetTableInfo(tableId);
            table.Attributes = GetTableAttributes(tableId);

            return table;
        }
        public int UpdateTableRow(int tableId, int rowId, Dictionary<string, dynamic> rowData)
        {
            Table table = GetTable(tableId);
            string query = $"update dbo.{table.SystemTableName} set ";

            List<AttributeType> attrTypes = GetAttributeTypes();
            var updatingAttrs = table.Attributes.Where(attr => attr.PkFlag != 1);
            foreach (TableAttribute attr in updatingAttrs)
            {
                if (rowData.ContainsKey(attr.SystemAttrName))
                {
                    query += $"{attr.SystemAttrName} = ";
                    if (rowData[attr.SystemAttrName] != null)
                    {
                        query += $"@{attr.SystemAttrName},";
                    }
                    else
                    {
                        query += $"NULL,";
                    }
                }
            }
            query = query.Remove(query.Length - 1);

            string pkAttr = table.Attributes.Find(attr => attr.PkFlag == 1)?.SystemAttrName;

            if (pkAttr is null)
            {
                return 0;
            }

            query += $" where {pkAttr} = @rowId;";

            int updatedRows = 0;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    foreach (TableAttribute attr in updatingAttrs)
                    {
                        if (rowData.ContainsKey(attr.SystemAttrName))
                        {
                            if (rowData[attr.SystemAttrName] is not null)
                            {
                                myCommand.Parameters.AddWithValue($"@{attr.SystemAttrName}", rowData[attr.SystemAttrName]);
                            }
                        }
                    }
                    myCommand.Parameters.AddWithValue("@rowId", rowId);
                    updatedRows = myCommand.ExecuteNonQuery();
                }
                myConn.Close();
            }
            return updatedRows;
        }
        public int InsertTableRow(int tableId, Dictionary<string, dynamic> rowData)
        {
            Table table = GetTable(tableId);
            string query = $"insert into dbo.{table.SystemTableName} (";
            string valuesQuery = "values (";

            List<AttributeType> attrTypes = GetAttributeTypes();
            var updatingAttrs = table.Attributes.Where(attr => attr.PkFlag != 1);
            foreach (TableAttribute attr in updatingAttrs)
            {
                if (rowData.ContainsKey(attr.SystemAttrName))
                {
                    query += $"{attr.SystemAttrName},";
                    if (rowData[attr.SystemAttrName] != null)
                    {
                        valuesQuery += $"@{attr.SystemAttrName},";
                    } else
                    {
                        valuesQuery += $"NULL,";
                    }
                }
            }
            valuesQuery = valuesQuery.Remove(valuesQuery.Length - 1);
            valuesQuery += ");";

            query = query.Remove(query.Length - 1);
            query += ") " + valuesQuery;

            int insertedRows = 0;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    foreach (TableAttribute attr in updatingAttrs)
                    {
                        if (rowData.ContainsKey(attr.SystemAttrName))
                        {
                            AttributeType attrType = attrTypes.Find(t => t.AttrTypeId == attr.AttrTypeId);
                            if (attrType is null)
                            {
                                throw new Exception("cannot find attribute type");
                            }
                            switch (attrType.SystemAttrTypeName)
                            {
                                case "date":
                                case "datetime2":
                                    if (rowData[attr.SystemAttrName] != null)
                                    {
                                        myCommand.Parameters.AddWithValue($"@{attr.SystemAttrName}", DateTime.Parse(rowData[attr.SystemAttrName]));
                                    }
                                    break;
                                case "varchar":
                                case "text":
                                case "int":
                                case "decimal":
                                case "join":
                                    myCommand.Parameters.AddWithValue($"@{attr.SystemAttrName}", rowData[attr.SystemAttrName]);
                                    break;
                                default:
                                    throw new Exception("unknown attribute type");
                            }
                        }
                    }

                    insertedRows = myCommand.ExecuteNonQuery();
                }
                myConn.Close();
            }
            return insertedRows;
        }
        public int DeleteTableRow(int tableId, int rowId)
        {
            Table table = GetTable(tableId);


            string pkAttr = table.Attributes.Find(attr => attr.PkFlag == 1)?.SystemAttrName;

            if (pkAttr is null)
            {
                return 0;
            }

            string query = $"delete from dbo.{table.SystemTableName} where {pkAttr} = @rowId;";

            int deletedRows = 0;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@rowId", rowId);
                    deletedRows = myCommand.ExecuteNonQuery();
                }
                myConn.Close();
            }
            return deletedRows;
        }
        public int UpdateTablesInfo(List<Table> tablesList)
        {
            string query = $"update dbo.UserTables set TableName = @tableName, CategoryId = @categoryId, Ord = @ord where TableId = @tableId;";
            int updatedRows = 0;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                foreach (Table table in tablesList)
                {
                    using (SqlCommand myCommand = new SqlCommand(query, myConn))
                    {
                        myCommand.Parameters.AddWithValue("@tableName", table.TableName);
                        myCommand.Parameters.AddWithValue("@categoryId", table.CategoryId);
                        myCommand.Parameters.AddWithValue("@ord", table.Ord);
                        myCommand.Parameters.AddWithValue("@tableId", table.TableId);
                        updatedRows += myCommand.ExecuteNonQuery();

                    }
                }
                myConn.Close();

            }
            return updatedRows;
        }
        private void updateTablesOrd(int categoryId, int ord)
        {
            string query = "update dbo.UserTables set Ord = Ord - 1 where CategoryId = @categoryId and Ord > @ord;";
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@categoryId", categoryId);
                    myCommand.Parameters.AddWithValue("@ord", ord);
                    myCommand.ExecuteNonQuery();
                }
                myConn.Close();
            }
        }
        public int UpdateTableInfo(Table table)
        {
            bool categoryChanged = false;
            int updatedRows = 0;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                // Get old CategoryId and Ord
                string getCategoryIdQuery = "select CategoryId, Ord from dbo.UserTables where TableId = @tableId;";
                using (SqlCommand myCommand = new SqlCommand(getCategoryIdQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@tableId", table.TableId);
                    SqlDataReader myReader = myCommand.ExecuteReader();
                    int oldCatId = 0;
                    int oldOrd = 0;
                    while (myReader.Read())
                    {
                        oldCatId = myReader.GetInt32(0);
                        oldOrd = myReader.GetInt32(1);
                    }
                    myReader.Close();

                    // If CategoryId changed, renew ord on all other tables with old CategoryId
                    if (oldCatId != table.CategoryId)
                    {
                        updateTablesOrd(oldCatId, oldOrd);
                        categoryChanged = true;
                    }

                }

                // Update info
                string ordStr = categoryChanged ? "(select (coalesce(max(ord),0) + 1) from dbo.UserTables where CategoryId = @categoryId)" : "@ord";
                string query = $"update dbo.UserTables set TableName = @tableName, CategoryId = @categoryId, Ord = {ordStr} where TableId = @tableId;";
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@tableName", table.TableName);
                    myCommand.Parameters.AddWithValue("@categoryId", table.CategoryId);
                    myCommand.Parameters.AddWithValue("@tableId", table.TableId);
                    if (!categoryChanged)
                    {
                        myCommand.Parameters.AddWithValue("@ord", table.Ord);
                    }
                    updatedRows = myCommand.ExecuteNonQuery();
                }

                myConn.Close();
            }

            return updatedRows;
        }
        private int InsertTableInfo(Table table)
        {
            string query = "insert into dbo.UserTables (SystemTableName, TableName, CategoryId, Ord) output INSERTED.TableId values (@systemTableName, @tableName, @categoryId, (select (coalesce(max(ord),0) + 1) from dbo.UserTables where CategoryId = @categoryId) )";
            int newId = 0;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@systemTableName", table.SystemTableName);
                    myCommand.Parameters.AddWithValue("@tableName", table.TableName);
                    myCommand.Parameters.AddWithValue("@categoryId", table.CategoryId);
                    newId = (int)myCommand.ExecuteScalar();
                }
                myConn.Close();

            }
            return newId;
        }
        public int CreateTable(Table table)
        {
            string systemTableName = Converter.ConvertToLatin(table.TableName);
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                // Check if table already exists
                string checkQuery = @"select count(*) from INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' and TABLE_NAME = @systemTableName";
                using (SqlCommand myCommand = new SqlCommand(checkQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@systemTableName", systemTableName);
                    int checkCount = (int)myCommand.ExecuteScalar();
                    if (checkCount > 0)
                    {
                        return 200;
                    }
                }

                // Create table in DB
                string idAttrName = $"{systemTableName}Id";
                string createQuery = $"create table dbo.[{systemTableName}] ({idAttrName} int identity(1,1));";
                using (SqlCommand myCommand = new SqlCommand(createQuery, myConn))
                {
                    int checkCreated = myCommand.ExecuteNonQuery();
                    if (checkCreated == 0)
                    {
                        return 300;
                    }
                }

                // Insert table info into dbo.UserTables
                table.SystemTableName = systemTableName;
                int newId = InsertTableInfo(table);
                if (newId == 0)
                {
                    return 400;
                }
                table.TableId = newId;

                // Insert Id attr info into dbo.Attributes
                string attrQuery = $"insert into dbo.Attributes (TableId, SystemAttrName, AttrName, PkFlag, AttrTypeId, Ord) output INSERTED.AttrId values (@tableId, @systemAtrName, @attrName, @pkFlag, @attrTypeId, @ord);";
                using (SqlCommand myCommand = new SqlCommand(attrQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@tableId", table.TableId);
                    myCommand.Parameters.AddWithValue("@systemAtrName", idAttrName);
                    myCommand.Parameters.AddWithValue("@attrName", "ID " + table.TableName);
                    myCommand.Parameters.AddWithValue("@pkFlag", 1);
                    myCommand.Parameters.AddWithValue("@attrTypeId", 3);
                    myCommand.Parameters.AddWithValue("@ord", 0);
                    int attrId = 0;
                    attrId = (int)myCommand.ExecuteScalar();
                    if (attrId == 1)
                    {
                        return 500;
                    }
                }

                myConn.Close();

            }
            return 100;
        }
        public void DeleteTable(int tableId)
        {

            // Get table info
            Table table = GetTableInfo(tableId);
            if (table.TableName is null)
            {
                throw new Exception();
            }

            string dropQuery = $"drop table dbo.[{table.SystemTableName}];";
            string deleteAttrsQuery = "delete from dbo.Attributes where TableId = @tableId;";
            string deleteQuery = "delete from dbo.UserTables where TableId = @tableId;";
            string updateOrdQuery = "update dbo.UserTables set Ord = Ord - 1 where CategoryId = @categoryId and Ord > @ord;";

            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();

                // Drop table from DB
                using (SqlCommand myCommand = new SqlCommand(dropQuery, myConn))
                {
                    myCommand.ExecuteNonQuery();
                }

                // Deleting table attrs from dbo.Attributes
                using (SqlCommand myCommand = new SqlCommand(deleteAttrsQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@tableId", tableId);
                    myCommand.ExecuteNonQuery();

                }

                // Delete table from dbo.UserTables
                using (SqlCommand myCommand = new SqlCommand(deleteQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@tableId", tableId);
                    myCommand.ExecuteNonQuery();

                }

                // Updating Ord of other tables which have same CategoryId
                using (SqlCommand myCommand = new SqlCommand(updateOrdQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@categoryId", table.CategoryId);
                    myCommand.Parameters.AddWithValue("@ord", table.Ord);
                    myCommand.ExecuteNonQuery();

                }

                myConn.Close();

            }
        }
        public int UpdateTablesAttributesOrd(List<TableAttribute> attributes)
        {
            string query = $"update dbo.Attributes set Ord = @ord where AttrId = @attrId;";
            int updatedRows = 0;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                foreach (TableAttribute attr in attributes)
                {
                    using (SqlCommand myCommand = new SqlCommand(query, myConn))
                    {
                        myCommand.Parameters.AddWithValue("@ord", attr.Ord);
                        myCommand.Parameters.AddWithValue("@attrId", attr.AttrId);
                        updatedRows += myCommand.ExecuteNonQuery();

                    }
                }
                myConn.Close();

            }
            return updatedRows;
        }
        public List<AttributeType> GetAttributeTypes()
        {
            string query = "select AttrTypeId, SystemAttrTypeName, AttrTypeName, Comment from dbo.AttrTypes order by AttrTypeId;";
            List<AttributeType> attrTypes = new List<AttributeType>();
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    SqlDataReader myReader = myCommand.ExecuteReader();
                    while (myReader.Read())
                    {
                        AttributeType attrType = new AttributeType()
                        {
                            AttrTypeId = myReader.GetInt32(myReader.GetOrdinal("AttrTypeId")),
                            SystemAttrTypeName = myReader.GetString(myReader.GetOrdinal("SystemAttrTypeName")),
                            AttrTypeName = myReader.GetString(myReader.GetOrdinal("AttrTypeName")),
                            Comment = myReader.GetString(myReader.GetOrdinal("Comment"))
                        };
                        attrTypes.Add(attrType);
                    }
                    myReader.Close();
                }
                myConn.Close();
            }
            return attrTypes;
        }
        public void UpdateAttributeName(TableAttribute attribute)
        {
            string query = "update dbo.Attributes set AttrName = @attrName where AttrId = @attrId";
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(query, myConn))
                {
                    myCommand.Parameters.AddWithValue("@attrName", attribute.AttrName);
                    myCommand.Parameters.AddWithValue("@attrId", attribute.AttrId);
                    myCommand.ExecuteNonQuery();
                }
                myConn.Close();
            }
        }
        public void UpdateAttributeWithType(string systemTableName, TableAttribute attribute)
        {
            string updateQuery = "update dbo.Attributes set AttrName = @attrName, AttrTypeId = @attrTypeId";
            string whereClause = " where AttrId = @attrId;";
            int updatedRows = 0;
            List<AttributeType> attrTypes = GetAttributeTypes();

            AttributeType attrType = attrTypes.Find(aType => aType.AttrTypeId == attribute.AttrTypeId);
            if (attrType is null)
            {
                throw new Exception("cannot find attribute type");
            }
            string dropColumnQuerty = $"alter table dbo.{systemTableName} drop column {attribute.SystemAttrName};";
            string alterQuery = $"alter table dbo.{systemTableName} add {attribute.SystemAttrName} ";
            switch (attrType.SystemAttrTypeName)
            {
                case "varchar":
                    alterQuery += $"varchar({attribute.AttrTypeProp1});";
                    updateQuery += ", AttrTypeProp1 = @attrTypeProp1, AttrTypeProp2 = NULL";
                    break;
                case "text":
                    alterQuery += $"text;";
                    updateQuery += ", AttrTypeProp1 = NULL, AttrTypeProp2 = NULL";
                    break;
                case "int":
                    alterQuery += $"int;";
                    updateQuery += ", AttrTypeProp1 = NULL, AttrTypeProp2 = NULL";
                    break;
                case "decimal":
                    alterQuery += $"decimal({attribute.AttrTypeProp1},{attribute.AttrTypeProp2});";
                    updateQuery += ", AttrTypeProp1 = @attrTypeProp1, AttrTypeProp2 = @attrTypeProp2";
                    break;
                case "date":
                    alterQuery += $"date;";
                    updateQuery += ", AttrTypeProp1 = NULL, AttrTypeProp2 = NULL";
                    break;
                case "datetime2":
                    alterQuery += $"datetime2;";
                    updateQuery += ", AttrTypeProp1 = NULL, AttrTypeProp2 = NULL";
                    break;
                case "join":
                    alterQuery += $"int;";
                    updateQuery += ", AttrTypeProp1 = @attrTypeProp1, AttrTypeProp2 = @attrTypeProp2";
                    break;
                default:
                    throw new Exception("uknown attribute type found");
            }
            updateQuery += whereClause;
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(dropColumnQuerty, myConn))
                {
                    myCommand.ExecuteNonQuery();
                }
                using (SqlCommand myCommand = new SqlCommand(alterQuery, myConn))
                {
                    myCommand.ExecuteNonQuery();
                }
                using (SqlCommand myCommand = new SqlCommand(updateQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@attrName", attribute.AttrName);
                    myCommand.Parameters.AddWithValue("@attrTypeId", attribute.AttrTypeId);
                    myCommand.Parameters.AddWithValue("@attrId", attribute.AttrId);
                    if (attrType.SystemAttrTypeName == "varchar" || attrType.SystemAttrTypeName == "decimal" || attrType.SystemAttrTypeName == "join")
                    {
                        myCommand.Parameters.AddWithValue("@attrTypeProp1", attribute.AttrTypeProp1);
                        if (attrType.SystemAttrTypeName == "decimal" || attrType.SystemAttrTypeName == "join")
                        {
                            myCommand.Parameters.AddWithValue("@attrTypeProp2", attribute.AttrTypeProp2);
                        }
                    }
                    updatedRows = myCommand.ExecuteNonQuery();
                }
                myConn.Close();
            }
        }
        public void AddAttribute(string systemTableName, TableAttribute attribute)
        {
            string systemAttrName = Converter.ConvertToLatin(attribute.AttrName);
            string insertQuery = "insert into dbo.Attributes (TableId, SystemAttrName, AttrName, PkFlag, AttrTypeId, Ord, AttrTypeProp1, AttrTypeProp2) " +
                "values (@tableId, @systemAttrName, @attrName, @pkFlag, @attrTypeId, (select (coalesce(max(ord),0) + 1) from dbo.Attributes where TableId = @tableId)";
            List<AttributeType> attrTypes = GetAttributeTypes();

            AttributeType attrType = attrTypes.Find(aType => aType.AttrTypeId == attribute.AttrTypeId);
            if (attrType is null)
            {
                throw new Exception("cannot find attribute type");
            }
            string alterQuery = $"alter table dbo.{systemTableName} add {systemAttrName} ";
            switch (attrType.SystemAttrTypeName)
            {
                case "varchar":
                    alterQuery += $"varchar({attribute.AttrTypeProp1});";
                    insertQuery += ", @attrTypeProp1, NULL);";
                    break;
                case "text":
                    alterQuery += $"text;";
                    insertQuery += ", NULL, NULL);";
                    break;
                case "int":
                    alterQuery += $"int;";
                    insertQuery += ", NULL, NULL);";
                    break;
                case "decimal":
                    alterQuery += $"decimal({attribute.AttrTypeProp1},{attribute.AttrTypeProp2});";
                    insertQuery += ", @attrTypeProp1, @attrTypeProp2);";
                    break;
                case "date":
                    alterQuery += $"date;";
                    insertQuery += ", NULL, NULL);";
                    break;
                case "datetime2":
                    alterQuery += $"datetime2;";
                    insertQuery += ", NULL, NULL);";
                    break;
                case "join":
                    alterQuery += $"int;";
                    insertQuery += ", @attrTypeProp1, @attrTypeProp2);";
                    break;
                default:
                    throw new Exception("uknown attribute type found");
            }
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(alterQuery, myConn))
                {
                    myCommand.ExecuteNonQuery();
                }
                using (SqlCommand myCommand = new SqlCommand(insertQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@tableId", attribute.TableId);
                    myCommand.Parameters.AddWithValue("@systemAttrName", systemAttrName);
                    myCommand.Parameters.AddWithValue("@attrName", attribute.AttrName);
                    myCommand.Parameters.AddWithValue("@pkFlag", 0);
                    myCommand.Parameters.AddWithValue("@attrTypeId", attribute.AttrTypeId);
                    if (attrType.SystemAttrTypeName == "varchar" || attrType.SystemAttrTypeName == "decimal" || attrType.SystemAttrTypeName == "join")
                    {
                        myCommand.Parameters.AddWithValue("@attrTypeProp1", attribute.AttrTypeProp1);
                        if (attrType.SystemAttrTypeName == "decimal" || attrType.SystemAttrTypeName == "join")
                        {
                            myCommand.Parameters.AddWithValue("@attrTypeProp2", attribute.AttrTypeProp2);
                        }
                    }
                    myCommand.ExecuteNonQuery();
                }
                myConn.Close();
            }
        }
        public int DeleteAttribute(string systemTableName, TableAttribute attribute)
        {
            string checkQuery = "select count(*) from dbo.Attributes where AttrTypeId = @attrTypeId and AttrTypeProp2 = @attrId;";
            string dropQuery = $"alter table dbo.{systemTableName} drop column {attribute.SystemAttrName};";
            string deleteQuery = "delete from dbo.Attributes where AttrId = @attrId;";
            string updateOrdQuery = "update dbo.Attributes set Ord = Ord - 1 where TableId = @tableId and Ord > @ord;";

            List<AttributeType> attrTypes = GetAttributeTypes();
            AttributeType attrType = attrTypes.Find(aType => aType.SystemAttrTypeName == "join");
            if (attrType is null)
            {
                throw new Exception("cannot find join attribute type");
            }
            using (SqlConnection myConn = new SqlConnection(sqlDataSource))
            {
                myConn.Open();
                using (SqlCommand myCommand = new SqlCommand(checkQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@attrTypeId", attrType.AttrTypeId);
                    myCommand.Parameters.AddWithValue("@attrId", attribute.AttrId);
                    int foundCount = (int)myCommand.ExecuteScalar();
                    if (foundCount > 0)
                    {
                        return foundCount;
                    }
                }
                using (SqlCommand myCommand = new SqlCommand(dropQuery, myConn))
                {
                    myCommand.ExecuteNonQuery();
                }
                using (SqlCommand myCommand = new SqlCommand(updateOrdQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@tableId", attribute.TableId);
                    myCommand.Parameters.AddWithValue("@ord", attribute.Ord);
                    myCommand.ExecuteNonQuery();
                }
                using (SqlCommand myCommand = new SqlCommand(deleteQuery, myConn))
                {
                    myCommand.Parameters.AddWithValue("@attrId", attribute.AttrId);
                    myCommand.ExecuteNonQuery();
                }
                myConn.Close();
            }
            return 0;
        }
        public TableAttribute GetTableAttribute(int attrId)
        {
            string queryAttr = "select AttrId, TableId, SystemAttrName, AttrName, PkFlag, AttrTypeId, AttrTypeProp1, AttrTypeProp2, Ord from dbo.Attributes where AttrId = @attrId;";
            TableAttribute attr = new TableAttribute();
            using (SqlConnection attrCon = new SqlConnection(sqlDataSource))
            {
                attrCon.Open();
                using (SqlCommand attrCommand = new SqlCommand(queryAttr, attrCon))
                {
                    attrCommand.Parameters.AddWithValue("@attrId", attrId);
                    SqlDataReader attrReader = attrCommand.ExecuteReader();
                    while (attrReader.Read())
                    {
                        attr = new TableAttribute
                        {
                            AttrId = attrReader.GetInt32(attrReader.GetOrdinal("AttrId")),
                            TableId = attrReader.GetInt32(attrReader.GetOrdinal("TableId")),
                            SystemAttrName = attrReader.GetString(attrReader.GetOrdinal("SystemAttrName")),
                            AttrName = attrReader.GetString(attrReader.GetOrdinal("AttrName")),
                            PkFlag = attrReader.GetInt32(attrReader.GetOrdinal("PkFlag")),
                            AttrTypeId = attrReader.GetInt32(attrReader.GetOrdinal("AttrTypeId")),
                            AttrTypeProp1 = SqlReader.GetNullableInt(attrReader, attrReader.GetOrdinal("AttrTypeProp1")),
                            AttrTypeProp2 = SqlReader.GetNullableInt(attrReader, attrReader.GetOrdinal("AttrTypeProp2")),
                            Ord = attrReader.GetInt32(attrReader.GetOrdinal("Ord"))
                        };
                    }
                    attrReader.Close();
                }
                attrCon.Close();
            }

            return attr;
        }
    }
}
