using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SilentCarApp.Constants;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using static SilentCarApp.Controllers.UsersController;

namespace SilentCarApp.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RoutesController : ControllerBase
    {
        private IConfiguration _configuration;

        public RoutesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Authorize]
        public JsonResult GetRoutes()
        {
            var query = "select * from dbo.Routes";
            var table = new DataTable();
            var sqlDatasource = _configuration.GetConnectionString("SilentCarAppDBCon");

            using var myCon = new SqlConnection(sqlDatasource);
            myCon.Open();

            using var myCommand = new SqlCommand(query, myCon);

            var myReader = myCommand.ExecuteReader();
            table.Load(myReader);
            myReader.Close();
            myCon.Close();

            return new JsonResult(table);
        }

        [HttpPost]
        [Authorize(Roles = "Driver")]
        public JsonResult AddRoutes([FromForm] string start_point, [FromForm] string end_point, [FromForm] string driver_id)
        {
            var driverID = driver_id;

            var query = "insert into dbo.Routes values(@newStart_Point, @newEnd_Point, @currentDriver_ID)";
            var table = new DataTable();
            var sqlDatasource = _configuration.GetConnectionString("SilentCarAppDBCon");

            using var myCon = new SqlConnection(sqlDatasource);
            myCon.Open();

            using var myCommand = new SqlCommand(query, myCon);

            myCommand.Parameters.AddWithValue("@newStart_Point", start_point);
            myCommand.Parameters.AddWithValue("@newEnd_Point", end_point);
            myCommand.Parameters.AddWithValue("@currentDriver_ID", driver_id);

            var myReader = myCommand.ExecuteReader();
            table.Load(myReader);
            myReader.Close();
            myCon.Close();

            return new(Messages.OK);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Driver")]
        public JsonResult DeleteRoutes(int id)
        {
            var query = "delete from dbo.Routes where id=@id";
            var table = new DataTable();
            var sqlDatasource = _configuration.GetConnectionString("SilentCarAppDBCon");

            using var myCon = new SqlConnection(sqlDatasource);
            myCon.Open();

            using var myCommand = new SqlCommand(query, myCon);

            myCommand.Parameters.AddWithValue("@id", id);

            var myReader = myCommand.ExecuteReader();
            table.Load(myReader);
            myReader.Close();
            myCon.Close();

            return new(Messages.OK);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Driver")]
        public JsonResult EditRoutes([FromForm] string newStart_point, [FromForm] string newEnd_point, int id)
        {
            var query = "update dbo.Routes set start_point = @newStart_point, end_point = @newEnd_point where id=@id";
            var table = new DataTable();
            var sqlDatasource = _configuration.GetConnectionString("SilentCarAppDBCon");

            using var myCon = new SqlConnection(sqlDatasource);
            myCon.Open();

            using var myCommand = new SqlCommand(query, myCon);

            myCommand.Parameters.AddWithValue("@id", id);
            myCommand.Parameters.AddWithValue("@newStart_Point", newStart_point);
            myCommand.Parameters.AddWithValue("@newEnd_Point", newEnd_point);

            var myReader = myCommand.ExecuteReader();
            table.Load(myReader);
            myReader.Close();
            myCon.Close();

            return new(Messages.OK);
        }

        [HttpGet("filters")]
        [Authorize(Roles = "Passenger")]
        public JsonResult FindRoutes([FromQuery] string start_point = "", [FromQuery] string end_point = "")
        {
            var query = "select * from dbo.routes";
            var table = new DataTable();
            var sqlDatasource = _configuration.GetConnectionString("SilentCarAppDBCon");

            using var myCon = new SqlConnection(sqlDatasource);

            if (start_point != string.Empty)
            {
                query += " where start_point = @start_point";
            }
            if (end_point != string.Empty)
            {
                if (start_point != string.Empty)
                {
                    query += " and end_point = @end_point";
                }
                else
                {
                    query += " where end_point = @end_point";
                }
            }

            myCon.Open();

            using var myCommand = new SqlCommand(query, myCon);
            if (start_point != string.Empty)
            {
                myCommand.Parameters.AddWithValue("@start_point", start_point);
            }
            if (end_point != string.Empty)
            {
                if (start_point != string.Empty)
                {
                    myCommand.Parameters.AddWithValue("@end_point", end_point);
                }
                else
                {
                    myCommand.Parameters.AddWithValue("@end_point", end_point);
                }
            }

            var myReader = myCommand.ExecuteReader();
            table.Load(myReader);
            myReader.Close();
            myCon.Close();

            return new JsonResult(table);

        }
        [HttpPost("checkin")]
        [Authorize(Roles = "Passenger")]
        public JsonResult CheckIn([FromForm] string user_id, [FromForm] string route_id) 
        {
            var exist = chekIfExist(user_id, route_id);

            if (exist == "1")
            {
                return new("Alredy Chek-ined");
            }
            else
            {
                var query = "insert into dbo.RoutesUsers values(@user_id, @route_id)";
                var table = new DataTable();
                var sqlDatasource = _configuration.GetConnectionString("SilentCarAppDBCon");

                using var myCon = new SqlConnection(sqlDatasource);
                myCon.Open();

                using var myCommand = new SqlCommand(query, myCon);

                myCommand.Parameters.AddWithValue("@user_id", user_id);
                myCommand.Parameters.AddWithValue("@route_id", route_id);

                var myReader = myCommand.ExecuteReader();
                table.Load(myReader);
                myReader.Close();
                myCon.Close();
            }                     
            return new(Messages.OK);
        }

        private string chekIfExist(string user_id, string route_id)
        {
            var query = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.routesusers WHERE user_id=@user_id AND route_id=@route_id) THEN 1 ELSE 0 END AS record_exists";
            var table = new DataTable();
            var sqlDatasource = _configuration.GetConnectionString("SilentCarAppDBCon");

            using var myCon = new SqlConnection(sqlDatasource);
            myCon.Open();

            using var myCommand = new SqlCommand(query, myCon);

            myCommand.Parameters.AddWithValue("@user_id", user_id);
            myCommand.Parameters.AddWithValue("@route_id", route_id);

            var myReader = myCommand.ExecuteReader();
            table.Load(myReader);
            myReader.Close();
            myCon.Close();

            var convertedList = (from rw in table.AsEnumerable() select rw.ItemArray[0].ToString()).Single();           

            return (convertedList);
        }
    }
}
