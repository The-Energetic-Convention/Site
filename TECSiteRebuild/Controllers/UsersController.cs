using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TECSite.Models;
using TECEncryption;
using Newtonsoft.Json;
using TECDataClasses;
using System;
using TECSite.EmailService;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TECSite.Controllers
{
    public class UsersController : Controller
    {
        private readonly ILogger<UsersController> _logger;
        public static string rResponse = "";
        public static UserData? user = null;

        public UsersController(ILogger<UsersController> logger)
        {
            _logger = logger;
        }

        public new IActionResult User(string? id)
        {
            string? authHeader = Request.Headers.Authorization;
            user = null;
            rResponse = "";
            switch (id)
            {
                case "Me":
                case null:
                    Console.WriteLine("Redirect to Me");
                    return Redirect($"{Program.domain}/Users/Me");
                default:
                    try // find user by id
                    { 
                        Console.WriteLine($"User {int.Parse(id)} page");

                        // get the event to remove
                        var (ss, pipeClient) = Program.ConnectClient();

                        // tell the server we are reading
                        ss.WriteString("R");
                        Program.CheckResponse(ss);

                        // tell the server it is an event
                        ss.WriteString("U");
                        Program.CheckResponse(ss);

                        ss.WriteString($"{id}");
                        user = JsonConvert.DeserializeObject<UserData>(ss.ReadString())!;
                        if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
                        pipeClient.Close();

                        string domain = Request.Host.Host.Split('.')[0];
                        if (domain == "api")
                        {
                            if (user == null) { return new NotFoundResult(); }
                            Console.WriteLine(Request.Headers.Authorization);
                            string[] authparts = ((string)Request.Headers.Authorization ?? "NULL NULL").Split(' ');

                            if (authparts[1] == Program.authKey && authparts[0] == "TEC_AUTH") { return new JsonResult(user); }
                            return new JsonResult(new Dictionary<string, object>() { { "username", user.username }, { "role", user.role }, { "pronouns", user.pronouns }, { "userID", user.userID } });
                        }

                        // No user found redirect to Me
                        if (user == null || (Request.Cookies.ContainsKey("loggedIn") && user.username == Encryption.Decrypt(Request.Cookies["loggedIn"]!, JsonConvert.DeserializeObject<byte[]>(Program.authKey)))) 
                            { return Redirect($"{Program.domain}/Users/Me"); }

                        return View(); 
                    }
                    catch (Exception e) // find user by username
                    {
                        Console.WriteLine($"{id} User page");

                        // get the event to remove
                        var (ss, pipeClient) = Program.ConnectClient();

                        // tell the server we are reading
                        ss.WriteString("R");
                        Program.CheckResponse(ss);

                        // tell the server it is an event
                        ss.WriteString("U");
                        Program.CheckResponse(ss);

                        ss.WriteString($"USERNAME={id}");
                        user = JsonConvert.DeserializeObject<UserData>(ss.ReadString())!;
                        if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
                        pipeClient.Close();

                        // No user found redirect to Me
                        if (user == null || (Request.Cookies.ContainsKey("loggedIn") && user.username == Encryption.Decrypt(Request.Cookies["loggedIn"]!, JsonConvert.DeserializeObject<byte[]>(Program.authKey))))
                            { return Redirect($"{Program.domain}/Users/Me"); }

                        return View(); 
                    }
            }
        }

        public IActionResult Me(
            string? uname = null,
            string? disuname = null,
            string? email = null,
            string? pronouns = null,
            string? birthdaystr = null,
            string? psw = null,
            bool remember = true)
        {
            user = null;
            rResponse = "";
            // get the current users info
            Console.WriteLine("Me");
            if (!Request.Cookies.ContainsKey("loggedIn")) { return Redirect($"{Program.domain}/Users/Login"); }

            string olduname = Encryption.Decrypt(Request.Cookies["loggedIn"]!, JsonConvert.DeserializeObject<byte[]>(Program.authKey));

            var (ss, pipeClient) = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is a user
            ss.WriteString("U");
            Program.CheckResponse(ss);

            ss.WriteString($"USERNAME={olduname}");
            user = JsonConvert.DeserializeObject<UserData>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { rResponse = "Error getting info"; }
            pipeClient.Close();

            string domain = Request.Host.Host.Split('.')[0];

            if (domain == "api")
            {
                if (user == null) { return new NotFoundResult(); }
                return new JsonResult(user);
            }

            if (uname == null)
            {
                return View();
            }


            string encryptedpsw = Encryption.Encrypt(psw, JsonConvert.DeserializeObject<byte[]>(Program.authKey));
            DateTime birthday = DateTime.ParseExact(birthdaystr!, "dd/MM/yyyy", null);
            if (user.encryptedPassword != encryptedpsw) { rResponse = "Incorrect password"; return View(); }

            if (uname != olduname)
            {
                (ss, pipeClient) = Program.ConnectClient();

                // tell the server we are reading
                ss.WriteString("R");
                Program.CheckResponse(ss);

                // tell the server it is a user
                ss.WriteString("U");
                Program.CheckResponse(ss);

                ss.WriteString($"USERNAME={uname}");
                string userexists = ss.ReadString();
                if (ss.ReadString() != "SUCCESS") { }
                pipeClient.Close();

                if (userexists != null) { rResponse = "Username taken"; return View(); }
            }

            UserData newuser = new(
                        uname,
                        disuname!,
                        encryptedpsw,
                        email!,
                        user.role,
                        user.emailConfirmed,
                        pronouns!,
                        birthday,
                        user.userID);

            // add them to the database
            (ss, pipeClient) = Program.ConnectClient();

            // tell the server we are updating
            ss.WriteString("U");
            Program.CheckResponse(ss);

            // tell the server it is a user
            ss.WriteString("U");
            Program.CheckResponse(ss);

            ss.WriteString(JsonConvert.SerializeObject(newuser));
            if (ss.ReadString() != "SUCCESS") { rResponse = "Error updating account"; return View(); }

            pipeClient.Close();

            rResponse = "Success!";
            if (uname != olduname)
            {
                CookieOptions cookieOptions = new();
                if (remember)
                {
                    cookieOptions.Expires = DateTime.UtcNow.AddMonths(6);
                }
                Response.Cookies.Append("loggedIn", Encryption.Encrypt(uname, JsonConvert.DeserializeObject<byte[]>(Program.authKey)), cookieOptions);
            }

            return Redirect($"{Program.domain}/Home");
        }
        public IActionResult Login(string? uname = null, string? psw = null, bool remember = true)
        {
            user = null;
            rResponse = "";
            // Serve login page, or log the user in
            Console.WriteLine($"{uname??""} Login");

            if (uname != null && psw != null)
            {
                string encryptedpsw = Encryption.Encrypt(psw, JsonConvert.DeserializeObject<byte[]>(Program.authKey));

                string domain = Request.Host.Host.Split('.')[0];

                var (ss, pipeClient) = Program.ConnectClient();

                // tell the server we are reading
                ss.WriteString("R");
                Program.CheckResponse(ss);

                // tell the server it is a user
                ss.WriteString("U");
                Program.CheckResponse(ss);

                ss.WriteString($"USERNAME={uname}");
                UserData user = JsonConvert.DeserializeObject<UserData>(ss.ReadString())!;
                if (ss.ReadString() != "SUCCESS") 
                { 
                    rResponse = "Incorrect username or password";

                    if (domain == "api")
                    {
                        return new BadRequestResult();
                    }

                    return View(); 
                }
                pipeClient.Close();

                if (user.encryptedPassword != encryptedpsw) 
                { 
                    rResponse = "Incorrect username or password";

                    if (domain == "api")
                    {
                        return new BadRequestResult();
                    }

                    return View(); 
                }

                rResponse = "Success!";
                CookieOptions cookieOptions = new();
                if (remember)
                {
                    cookieOptions.Expires = DateTime.UtcNow.AddMonths(6);
                }
                Response.Cookies.Append("loggedIn", Encryption.Encrypt(uname, JsonConvert.DeserializeObject<byte[]>(Program.authKey)), cookieOptions);

                if (domain == "api")
                {
                    return new OkResult();
                }

                return Redirect($"{Program.domain}/Home");
            }

            return View();
        }
        public IActionResult Logout(string? confirm = null)
        {
            user = null;
            rResponse = "";
            // log the user out
            Console.WriteLine("Logout");
            Console.WriteLine(confirm);

            string domain = Request.Host.Host.Split('.')[0];
            if (domain == "api")
            {
                Response.Cookies.Delete("loggedIn");
                return new OkResult();
            }

            if (confirm != null) { Response.Cookies.Delete("loggedIn"); return Redirect($"{Program.domain}/Home"); }

            else { return View(); }
        }
        public IActionResult Register(
            string? uname = null,
            string? disuname = null,
            string? email = null,
            string? pronouns = null,
            string? birthdaystr = null,
            string? psw = null,
            string? confpsw = null,
            bool remember = true
            )
        {
            user = null;
            rResponse = "";
            // serve the registration page, or register the user
            Console.WriteLine("Register");

            if (uname == null) { return View(); }
            else if (psw != confpsw) { rResponse = "Passwords do not match"; return View(); }
            else
            {
                string encryptedPsw = Encryption.Encrypt(psw, JsonConvert.DeserializeObject<byte[]>(Program.authKey));
                DateTime birthday = DateTime.ParseExact(birthdaystr!, "dd/MM/yyyy", null);

                // check if username taken
                var (ss, pipeClient) = Program.ConnectClient();

                // tell the server we are reading
                ss.WriteString("R");
                Program.CheckResponse(ss);

                // tell the server it is a user
                ss.WriteString("U");
                Program.CheckResponse(ss);

                ss.WriteString($"USERNAME={uname}");
                UserData? userexists = JsonConvert.DeserializeObject<UserData>(ss.ReadString());
                if (ss.ReadString() != "SUCCESS") { }
                pipeClient.Close();

                Console.WriteLine(JsonConvert.SerializeObject(userexists));
                if (userexists != null) { rResponse = "Username taken"; return View(); }

                // get accounts
                (ss, pipeClient) = Program.ConnectClient();

                // tell the server we are reading
                ss.WriteString("R");
                Program.CheckResponse(ss);

                // tell the server it is a user
                ss.WriteString("U");
                Program.CheckResponse(ss);

                ss.WriteString("ALL");
                UserData[] users = JsonConvert.DeserializeObject<UserData[]>(ss.ReadString())!;
                if (ss.ReadString() != "SUCCESS") { rResponse = "Server Error"; return View(); }
                pipeClient.Close();

                UserData user = new(
                        uname,
                        disuname!,
                        encryptedPsw,
                        email!,
                        UserRole.user,
                        false,
                        pronouns!,
                        birthday,
                        users.Length);

                // add them to the database
                (ss, pipeClient) = Program.ConnectClient();

                // tell the server we are creating
                ss.WriteString("C");
                Program.CheckResponse(ss);

                // tell the server it is a user
                ss.WriteString("U");
                Program.CheckResponse(ss);

                ss.WriteString(JsonConvert.SerializeObject(user));
                if (ss.ReadString() != "SUCCESS") { rResponse = "Error creating account"; return View(); }

                pipeClient.Close();

                rResponse = "Success!";
                CookieOptions cookieOptions = new();
                if (remember)
                {
                    cookieOptions.Expires = DateTime.UtcNow.AddMonths(6);
                }
                Response.Cookies.Append("loggedIn", Encryption.Encrypt(uname, JsonConvert.DeserializeObject<byte[]>(Program.authKey)), cookieOptions);

                Console.WriteLine("getting sender");
                EmailSender emailSender = new EmailSender();
                Console.WriteLine("setting email and message");

                int code = new Random().Next(103852, 962957);

                Console.WriteLine("setting to dict");
                Dictionary<string, string> nameadressdict = new() { { uname, email! } };
                Console.WriteLine("Making Message");
                var message = new Message("The Energetic Convention", "staff@thenergeticon.com", nameadressdict, "TEC Email Confirmation", $"Here is your confirmation code: {code} \nClick <a href=\"https://thenergeticon.com/Users/ConfirmEmail/{code}\">here</a> to confirm", null);
                Console.WriteLine("Sending Message");
                emailSender.SendEmail(message);

                string filepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TECSite\\EmailsToConfirm.json";
                Dictionary<string, int> emails = [];
                if (System.IO.File.Exists(filepath))
                {
                    emails = JsonConvert.DeserializeObject<Dictionary<string, int>>(System.IO.File.ReadAllText(filepath)) ?? [];
                }
                emails.Add(email!, code);

                System.IO.File.WriteAllText(filepath, JsonConvert.SerializeObject(emails));

                return Redirect($"{Program.domain}/Users/ConfirmEmail");
            }
        }

        public IActionResult ConfirmEmail(int? code = null)
        {
            user = null;
            rResponse = "";
            // confirm the user's email
            Console.WriteLine("Confirm Email");

            if (!Request.Cookies.ContainsKey("loggedIn")) { return Redirect($"{Program.domain}/Users/Login"); }

            string uname = Encryption.Decrypt(Request.Cookies["loggedIn"]!, JsonConvert.DeserializeObject<byte[]>(Program.authKey));

            var (ss, pipeClient) = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is a user
            ss.WriteString("U");
            Program.CheckResponse(ss);

            ss.WriteString($"USERNAME={uname}");
            user = JsonConvert.DeserializeObject<UserData>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { rResponse = "Error getting info"; }
            pipeClient.Close();

            if (user.emailConfirmed) { return Redirect($"{Program.domain}/Home"); }

            if (code != null)
            {
                // check code, if good then email confirmed
                string filepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TECSite\\EmailsToConfirm.json";
                Dictionary<string, int> emails = [];
                if (System.IO.File.Exists(filepath))
                {
                    emails = JsonConvert.DeserializeObject<Dictionary<string, int>>(System.IO.File.ReadAllText(filepath)) ?? [];
                }

                if (emails.ContainsKey(user.email))
                {
                    if (emails[user.email] == code)
                    {
                        user.emailConfirmed = true;

                        // add them to the database
                        (ss, pipeClient) = Program.ConnectClient();

                        // tell the server we are updating
                        ss.WriteString("U");
                        Program.CheckResponse(ss);

                        // tell the server it is a user
                        ss.WriteString("U");
                        Program.CheckResponse(ss);

                        ss.WriteString(JsonConvert.SerializeObject(user));
                        if (ss.ReadString() != "SUCCESS") { rResponse = "Error updating account"; return View(); }

                        pipeClient.Close();

                        emails.Remove(user.email);

                        System.IO.File.WriteAllText(filepath, JsonConvert.SerializeObject(emails));

                        return Redirect($"{Program.domain}/Home");
                    }

                    rResponse = "Incorrect code!";
                    return View();
                }
                rResponse = "Email does not needing to be confirmed";
                return Redirect($"{Program.domain}/Home");
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
