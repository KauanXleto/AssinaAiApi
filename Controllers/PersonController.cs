using AssinaAiApi.Entities;
using AssinaAiApi.Models;
using AssinaAiApi.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AssinaAiApi.Controllers
{
    public static class PersonController
    {
        [EndpointDescription("EndPoins da Pessoa")]
        public static void ConfigurePersonApi(this WebApplication app)
        {
            app.MapPost("Add/Person", AddPerson)
            .WithName("AddPerson")
            .WithDescription("Adicionar Pessoas")
            .WithOpenApi();

            app.MapGet("/Person", GetPerson)
            .WithName("GetPerson")
            .WithDescription("Consultar pessoas")
            .WithOpenApi();

            app.MapGet("/Person/{id}", GetPersonById)
            .WithName("GetPersonById")
            .WithDescription("Consultar pessoas pelo Id")
            .WithOpenApi();

            app.MapPost("/Person/{id}", UpdatePerson)
            .WithName("UpdatePerson")
            .WithDescription("Atualizar pessoas")
            .WithOpenApi();

            app.MapDelete("/Person/{id}", DeletePerson)
            .WithName("DeletePerson")
            .WithDescription("Deletar pessoas")
            .WithOpenApi();
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<List<Person>> GetPerson(DataContext context) => await context.Person.ToListAsync();

        public static bool validTelephoneNo(string telNo)
        {
            return Regex.Match(telNo, @"^(1[1-9]|2[12478]|3([1-5]|[7-8])|4[1-9]|5(1|[3-5])|6[1-9]|7[134579]|8[1-9]|9[1-9])9[0-9]{8}$").Success;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<IResult> AddPerson(DataContext context, PersonModel entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Name) || entity.Name.Length < 4)
                return Results.Problem("Nome inválido");

            if (string.IsNullOrWhiteSpace(entity.PhoneNumber) || entity.PhoneNumber.Length < 4 || !validTelephoneNo(entity.PhoneNumber))
                return Results.Problem("Celular inválido");


            var item = new Person()
            {
                Name = entity.Name,
                PhoneNumber = entity.PhoneNumber,
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now,
                UniqueId = Guid.NewGuid()
            };

            context.Person.Add(item);
            await context.SaveChangesAsync();

            return Results.Ok(await GetPerson(context));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<IResult> GetPersonById(DataContext context, int id)
        {
            return Results.Ok(await context.Person.FindAsync(id));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<IResult> UpdatePerson(DataContext context, PersonModel entity, int id)
        {
            var person = await context.Person.FindAsync(id);

            if (person != null && person.Id > 0)
            {
                if (string.IsNullOrWhiteSpace(entity.Name) || entity.Name.Length < 4)
                    return Results.Problem("Nome inválido");

                if (string.IsNullOrWhiteSpace(entity.PhoneNumber) || entity.PhoneNumber.Length < 4 || !validTelephoneNo(entity.PhoneNumber))
                    return Results.Problem("Celular inválido");

                person.Name = entity.Name;
                person.PhoneNumber = entity.PhoneNumber;
                person.UpdateDate = DateTime.Now;

                context.Person.Update(person);
                await context.SaveChangesAsync();
            }
            else
                return Results.NotFound("Pessoa não encontra");

            return Results.Ok(await GetPerson(context));
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<IResult> DeletePerson(DataContext context, int id)
        {
            var person = await context.Person.FindAsync(id);

            if (person != null && person.Id > 0)
            {
                context.Remove(person);
                await context.SaveChangesAsync();
            }
            else
                return Results.NotFound("Pessoa não encontra");

            return Results.Ok(await GetPerson(context));
        }
    }
        
}
