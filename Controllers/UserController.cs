using AssinaAi.BusinessEntities;
using AssinaAiApi.Models;
using AssinaAiApi.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AssinaAiApi.Controllers
{
    public static class UserController
    {
        private static WebApplication currentContext;
        public static void ConfigureUserApi(this WebApplication app)
        {
            currentContext = app;

            app.MapPost("Add/User", AddUser)
            .WithName("AddUser")
            .WithOpenApi();

            app.MapGet("/User", GetUser)
            .WithName("GetUser")
            .WithOpenApi();

            app.MapGet("/User/{id}", GetUserById)
            .WithName("GetUserById")
            .WithOpenApi();

            app.MapPost("/User/{id}", UpdateUser)
            .WithName("UpdateUser")
            .WithOpenApi();

            app.MapDelete("/User/{id}", DeleteUser)
            .WithName("DeleteUser")
            .WithOpenApi();

            app.MapPost("/LogOn", LogOn)
            .WithName("LogOn")
            .WithOpenApi();
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<List<UserInfo>> GetUser(DataContext context) => await context.UserInfo.ToListAsync();

        public static bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();

            // Check if the email ends with a dot (suggested by @TK-421)
            if (trimmedEmail.EndsWith("."))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }
        private static string ComputeHash(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var inputHash = SHA256.HashData(inputBytes);
            return Convert.ToHexString(inputHash);
        }
        public static bool validTelephoneNo(string telNo)
        {
            return Regex.Match(telNo, @"^(1[1-9]|2[12478]|3([1-5]|[7-8])|4[1-9]|5(1|[3-5])|6[1-9]|7[134579]|8[1-9]|9[1-9])9[0-9]{8}$").Success;
        }

        private static async Task<IResult> AddUser(DataContext context, UserModel entity)
        {
            var usersSameEmail = await context.UserInfo.Where(x => x.Email == entity.Email).FirstOrDefaultAsync();

            if(usersSameEmail != null && usersSameEmail.Id > 0)
                return Results.Problem("Email inválido");

            if(!IsValidEmail(entity.Email))
                return Results.Problem("Email inválido");

            if (string.IsNullOrWhiteSpace(entity.Password) || entity.Password.Length < 4)
                return Results.Problem("Senha inválida");


            if(entity.Person == null)
                return Results.Problem("Informações inválidas");
            else
            {
                if(string.IsNullOrWhiteSpace(entity.Person.Name) || entity.Person.Name.Length < 4)
                    return Results.Problem("Nome inválido");

                if (string.IsNullOrWhiteSpace(entity.Person.PhoneNumber) || entity.Person.PhoneNumber.Length < 4 || !validTelephoneNo(entity.Person.PhoneNumber))
                    return Results.Problem("Celular inválido");
            }

            var item = new Person()
            {
                Name = entity.Person.Name,
                PhoneNumber = entity.Person.PhoneNumber,
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now,
                UniqueId = Guid.NewGuid()
            };

            context.Person.Add(item);
            await context.SaveChangesAsync();

            var user = new UserInfo()
            {
                PersonId = item.Id,
                Email = entity.Email,
                Role = "Usuario",
                Password = ComputeHash(entity.Password),
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now,
                UniqueId = Guid.NewGuid()
            };

            context.UserInfo.Add(user);
            await context.SaveChangesAsync();

            return Results.Ok(await LogOn(context, new AuthenticationModel() { Email= entity.Email, Password= entity.Password }));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<IResult> GetUserById(DataContext context, int id)
        {            
            return Results.Ok(await context.UserInfo.FindAsync(id));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<IResult> UpdateUser(DataContext context, UserModel entity, int id)
        {
            var user = await context.UserInfo.FindAsync(id);

            if (user != null && user.Id > 0)
            {
                user.Email = entity.Email;
                user.Password = entity.Password;

                user.UpdateDate = DateTime.Now;

                context.UserInfo.Update(user);
                await context.SaveChangesAsync();
            }
            else
                return Results.NotFound("Usuário não encontrado");

            return Results.Ok(await GetUser(context));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<IResult> DeleteUser(DataContext context, int id)
        {
            var user = await context.UserInfo.FindAsync(id);

            if (user != null && user.Id > 0)
            {
                var person = await context.Person.FindAsync(user.PersonId);

                context.Remove(user);
                await context.SaveChangesAsync();

                if (person != null && person.Id > 0)
                {
                    context.Remove(person);
                    await context.SaveChangesAsync();
                }
            }
            else
                return Results.NotFound("Usuário não encontra");

            return Results.Ok(await GetUser(context));
        }

        private static async Task<IResult> LogOn(DataContext context, AuthenticationModel entity)
        {

            var users = await context.UserInfo.ToListAsync();

            var hashPassword = ComputeHash(entity.Password);
            var masterPassword = currentContext.Configuration["MasterPassword"];

            var user = users.Where(x => x.Email == entity.Email && (x.Password == hashPassword || masterPassword == hashPassword)).FirstOrDefault();

            if (user != null && user.Id > 0)
            {
                user.Person = await context.Person.FindAsync(user.PersonId);

                var claims = new[]
                {
                        new Claim(ClaimTypes.NameIdentifier, user.Person.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Person.Name),
                        new Claim(ClaimTypes.Hash, user.Person.UniqueId.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, (string.IsNullOrWhiteSpace(user.Role) ? "admin" : user.Role)),
                    };

                var token = new JwtSecurityToken(
                    issuer: currentContext.Configuration["Jwt:Issuer"],
                    audience: currentContext.Configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddDays(60),
                    signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(currentContext.Configuration["Jwt:Key"])), SecurityAlgorithms.HmacSha256)

                );

                var result = new JwtSecurityTokenHandler().WriteToken(token);
                //result.Token = new JwtSecurityTokenHandler().WriteToken(token);

                return Results.Ok(result);
            }
            else
                return Results.NotFound("Usuário não encontrado");
        }
    }
}
