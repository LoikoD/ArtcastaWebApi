using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using ArtcastaWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ArtcastaWebApi.Services;

namespace ArtcastaWebApi.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class TableController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ITableService _tableService;

        public TableController(IConfiguration configuration, ITableService tableService)
        {
            _config = configuration;
            _tableService = tableService;
        }


        /// <summary>
        /// Метод GET для получения всех таблиц, содержащихся в БД
        /// </summary>
        /// <returns>JSON список объектов <see cref="Table"/>.</returns>
        [HttpGet]
        public JsonResult Get()
        {
            List<Table> tablesList = _tableService.GetTables();

            return new JsonResult(tablesList);
        }


        /// <summary>
        /// Метод PUT для обновления информации по нескольким таблицам, передаваемых в теле запроса
        /// </summary>
        /// <param name="tables">Список объектов <see cref="Table"/>, откуда будут браться данные для обновления</param>
        /// <returns>Код ответа 200, если обновление прошло успешно. Код ответа 400, если возникла ошибка</returns>
        [HttpPut]
        public ActionResult Put(List<Table> tables)
        {
            int updatedTables = _tableService.UpdateTablesInfo(tables);

            if (updatedTables == tables.Count)
            {
                return new OkResult();
            } else
            {
                return new BadRequestResult();
            }

        }

        /// <summary>
        /// Метод GET для получения <see cref="Table"/> по ID таблицы
        /// </summary>
        /// <param name="tableId">ID таблицы</param>
        /// <returns>JSON объект <see cref="Table"/></returns>
        [HttpGet("{tableId}")]
        public JsonResult Get(int tableId)
        {
            Table table = _tableService.GetTable(tableId);

            return new JsonResult(table);
        }

        /// <summary>
        /// Метод POST для создания таблицы
        /// </summary>
        /// <param name="table">Объект</param>
        /// <returns>Код ответа 200, если создание прошло успешно. Код ответа 500, если возникла ошибка</returns>
        [HttpPost]
        public ActionResult Post(Table table)
        {
            int result = _tableService.CreateTable(table);

            if (result == 100)
            {
                return new OkResult();
            } else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Service returned result: {result}");
            }
        }

        /// <summary>
        /// Метод PUT для обновления информации по одной таблице
        /// </summary>
        /// <param name="tableId">ID таблицы</param>
        /// <param name="table">Объект <see cref="Table"/>, содержащий информацию для обновления</param>
        /// <returns>Код ответа 200, если обновление прошло успешно. Код ответа 400, если возникла ошибка</returns>
        [HttpPut("{tableId}")]
        public ActionResult Put(int tableId, Table table)
        {
            if (tableId != table.TableId)
            {
                return new BadRequestResult();
            }

            int updatedTables = _tableService.UpdateTableInfo(table);

            if (updatedTables == 1)
            {
                return new OkResult();
            }
            else
            {
                return new BadRequestResult();
            }
        }

        /// <summary>
        /// Метод DELETE для удаления таблицы из БД
        /// </summary>
        /// <param name="tableId">ID таблицы</param>
        /// <returns>Код ответа 200, если удаление прошло успешно. Код ответа 500 с сообщением об ошибке, если она возникла</returns>
        [HttpDelete("{tableId}")]
        public ActionResult Delete(int tableId)
        {
            try
            {
                _tableService.DeleteTable(tableId);
                return new OkResult();
            } catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Метод POST для добавления строки в таблицу
        /// </summary>
        /// <param name="tableId">ID таблицы</param>
        /// <param name="jo">Объект, содержащий атрибуты, соответвующие полям таблицы</param>
        /// <returns>Код ответа 200, если добавление прошло успешно. Код ответа 400, если возникла ошибка</returns>
        [HttpPost("{tableId}/row")]
        public ActionResult Post(int tableId, dynamic jo)
        {
            Dictionary<string, dynamic> rowData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jo.ToString());


            int result = _tableService.InsertTableRow(tableId, rowData);

            if (result == 0)
            {
                return new BadRequestResult();
            }
            else
            {
                return new OkResult();
            }
        }

        /// <summary>
        /// Метод PUT для обновления данных в строке таблицы
        /// </summary>
        /// <param name="tableId">ID таблицы</param>
        /// <param name="rowId">ID строки (атрибут таблицы, с PkFlag = 1)</param>
        /// <param name="jo">Объект, содержащий атрибуты, соответвующие полям таблицы</param>
        /// <returns>Код ответа 200, если обновление прошло успешно. Код ответа 400, если возникла ошибка</returns>
        [HttpPut("{tableId}/row/{rowId}")]
        public ActionResult Put(int tableId, int rowId, dynamic jo)
        {
            Dictionary<string, dynamic> rowData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jo.ToString());


            int result = _tableService.UpdateTableRow(tableId, rowId, rowData);

            if (result == 0)
            {
                return new BadRequestResult();
            } else
            {
                return new OkResult();
            }

        }

        /// <summary>
        /// Метод DELETE для удаления строки из таблицы
        /// </summary>
        /// <param name="tableId">ID таблицы</param>
        /// <param name="rowId">ID строки (атрибут таблицы, с PkFlag = 1)</param>
        /// <returns>Код ответа 200, если удаление прошло успешно. Код ответа 400, если возникла ошибка</returns>
        [HttpDelete("{tableId}/row/{rowId}")]
        public ActionResult Delete(int tableId, int rowId)
        {


            int result = _tableService.DeleteTableRow(tableId, rowId);

            if (result == 0)
            {
                return new BadRequestResult();
            }
            else
            {
                return new OkResult();
            }

        }

        /// <summary>
        /// Метод PUT для обновления порядка атрибутов в таблице. Другая информация по атрибутам не обновляется
        /// </summary>
        /// <param name="attributes">Список объектов <see cref="TableAttribute"/></param>
        /// <returns>Код ответа 200, если удаление прошло успешно. Код ответа 400, если возникла ошибка</returns>
        [HttpPut("attribute")]
        public ActionResult Put(List<TableAttribute> attributes)
        {
            int result = _tableService.UpdateTablesAttributesOrd(attributes);
            if (result == 0)
            {
                return new BadRequestResult();
            }
            else
            {
                return new OkResult();
            }
        }

        /// <summary>
        /// Метод GET для получения данных о всех типах атрибутов
        /// </summary>
        /// <returns>Список объектов <see cref="AttributeType"/>. Код ответа 500, если возникла ошибка</returns>
        [HttpGet("attribute/type")]
        public ActionResult GetAttrTypes()
        {
            List<AttributeType> attributeTypes = _tableService.GetAttributeTypes();

            if (attributeTypes == null || attributeTypes.Count == 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            } else
            {
                return new JsonResult(attributeTypes);
            }
        }

        [HttpPut("attribute/{attrId}")]
        public ActionResult UpdateAttribute(int attrId, UpdateAttrRequestBody body)
        {
            if (body?.typeChanged is null || body?.attribute is null || attrId != body.attribute.AttrId)
            {
                return new BadRequestResult();
            }
            try
            {
                if (body.typeChanged == true)
                {
                    Table table = _tableService.GetTableInfo(body.attribute.TableId);
                    _tableService.UpdateAttributeWithType(table.SystemTableName, body.attribute);
                }
                else
                {
                    _tableService.UpdateAttributeName(body.attribute);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            return new OkResult();
        }

        [HttpPost("attribute")]
        public ActionResult CreateAttribute(TableAttribute attribute)
        {
            try
            {
                Table table = _tableService.GetTableInfo(attribute.TableId);
                _tableService.AddAttribute(table.SystemTableName, attribute);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            return new OkResult();
        }

        [HttpDelete("attribute/{attrId}")]
        public ActionResult DeleteAttribute(int attrId)
        {
            try
            {
                TableAttribute attribute = _tableService.GetTableAttribute(attrId);
                Table table = _tableService.GetTableInfo(attribute.TableId);
                int result = _tableService.DeleteAttribute(table.SystemTableName, attribute);
                if (result > 0 )
                {
                    return StatusCode(StatusCodes.Status409Conflict, "dependent attributes exist error");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            return new OkResult();
        }
    }
}
