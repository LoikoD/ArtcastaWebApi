using ArtcastaWebApi.Models;
using System.Data;

namespace ArtcastaWebApi.Services
{
    public interface ITableService
    {
        List<TableAttribute> GetTableAttributes(int tableId);
        //DataTable GetTableData(string tableName, List<TableAttribute> attributes);
        List<Table> GetTables();
        Table GetTableInfo(int tableId);
        Table GetTable(int tableId);

        /// <summary>
        /// Создание таблицы в БД
        /// </summary>
        /// <param name="table"></param>
        /// <returns>Статус выполнения функции: <br />
        /// 100 - таблица успешно создана,<br />
        /// 200 - таблица с таким именем уже существует,<br />
        /// 300 - не удалось выполнить команду create table,<br />
        /// 400 - не удалось вставить данные в UserTables,<br /> 
        /// 500 - не удалось добавить информацию об атрибуте ID в dbo.Attributes</returns>
        int CreateTable(Table table);

        int UpdateTablesInfo(List<Table> tablesList);
        
        // private now
        //int InsertTableInfo(Table table);
        int UpdateTableInfo(Table table);
        void DeleteTable(int tableId);

        int InsertTableRow(int tableId, Dictionary<string, dynamic> rowData);
        int UpdateTableRow(int tableId, int rowId, Dictionary<string, dynamic> rowData);
        int DeleteTableRow(int tableId, int rowId);
        int UpdateTablesAttributesOrd(List<TableAttribute> attributes);
        List<AttributeType> GetAttributeTypes();
        void UpdateAttributeName(TableAttribute attribute);
        void UpdateAttributeWithType(string systemTableName, TableAttribute attribute);
        void AddAttribute(string systemTableName, TableAttribute attribute);
        int DeleteAttribute(string systemTableName, TableAttribute attribute);
        TableAttribute GetTableAttribute(int attrId);
    }
}