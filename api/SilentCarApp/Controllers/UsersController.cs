using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SilentCarApp.Constants;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace SilentCarApp.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private IConfiguration _configuration;

        public UsersController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public class MyObj
        {
            public string name { get; set; }
            public int id { get; set; }
            public string role { get; set; }
        }

        private List<MyObj> TableToList(DataTable table)
        {

            var convertedList = (from rw in table.AsEnumerable()
            select new MyObj()
            {
                name = Convert.ToString(rw["name"]),
                id = Convert.ToInt32(rw["id"]),
                role = Convert.ToString(rw["role"])
            }).ToList();

            return convertedList;
        }

        private string Generate(string id, string name, string role)
        {
            var claims = new[]
            {
                new Claim("id", $"{id}"),
                new Claim("name", $"{name}"),
                new Claim("role", $"{role}")
            };

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var loginCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
            var tokenOptions = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(5),
                signingCredentials: loginCredentials
            );
            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            return tokenString;
        }

        [HttpPost]
        public JsonResult AddUsers([FromForm] string name, [FromForm] string phone_number, [FromForm] string password, [FromForm] Role role)
        {
            var query = "insert into dbo.Users values(@name, @phone_number, @password, @role)";
            var table = new DataTable();
            var sqlDatasource = _configuration.GetConnectionString("SilentCarAppDBCon");

            using var myCon = new SqlConnection(sqlDatasource);
            myCon.Open();

            using var myCommand = new SqlCommand(query, myCon);

            myCommand.Parameters.AddWithValue("@name", name);
            myCommand.Parameters.AddWithValue("@phone_number", phone_number);
            myCommand.Parameters.AddWithValue("@password", password);            
            myCommand.Parameters.AddWithValue("@role", role.ToString());

            var myReader = myCommand.ExecuteReader();
            table.Load(myReader);
            myReader.Close();
            myCon.Close();

            return new(Messages.OK);
        }

        [HttpGet]
        public JsonResult LoginUser([FromQuery] string phone_number, [FromQuery] string password)
        {
            var query = "select * from dbo.Users where phone_number=@phone_number and password=@password";
            var table = new DataTable();
            var sqlDatasource = _configuration.GetConnectionString("SilentCarAppDBCon");

            using var myCon = new SqlConnection(sqlDatasource);
            myCon.Open();

            using var myCommand = new SqlCommand(query, myCon);

            myCommand.Parameters.AddWithValue("@phone_number", phone_number);
            myCommand.Parameters.AddWithValue("@password", password);

            var myReader = myCommand.ExecuteReader();
            table.Load(myReader);
            myReader.Close();
            myCon.Close();

            var list = TableToList(table);
            var userName = list[0].name.ToString();
            var userID = list[0].id.ToString();
            var userRole = list[0].role.ToString();

            var token = Generate(userID, userName, userRole);

            return new(token);
        }       
    }
}
