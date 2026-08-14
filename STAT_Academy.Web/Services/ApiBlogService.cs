using System.Net.Http.Json;
using STAT_Academy.Web.Models;


namespace STAT_Academy.Web.Services
{
    public class ApiBlogService
    {

        private readonly HttpClient _http;


        public ApiBlogService(HttpClient http)
        {
            _http = http;
        }



        public async Task<List<BlogViewModel>> GetBlogs()
        {
            var response = await _http.GetFromJsonAsync<List<BlogViewModel>>(
                "api/Blog"
            );

            return response ?? new List<BlogViewModel>();
        }



        public async Task<BlogViewModel?> GetBlog(int id)
        {

            return await _http.GetFromJsonAsync<BlogViewModel>(
                $"api/Blog/{id}"
            );

        }



        public async Task Crear(BlogViewModel blog)
        {

            await _http.PostAsJsonAsync(
                "api/Blog",
                blog
            );

        }



        public async Task Actualizar(
            int id,
            BlogViewModel blog)
        {

            await _http.PutAsJsonAsync(
                $"api/Blog/{id}",
                blog
            );

        }



        public async Task Eliminar(int id)
        {

            await _http.PatchAsync(
                $"api/Blog/{id}/desactivar",
                null
            );

        }

    }
}