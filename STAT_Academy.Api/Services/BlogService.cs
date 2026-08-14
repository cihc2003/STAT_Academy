using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.DTOs.Blogs;


namespace STAT_Academy.Api.Services
{
    public class BlogService
    {

        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;


        public BlogService(
            ApplicationDbContext context,
            AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
        }



        public List<BlogResponse> GetBlogs()
        {
            return _context.EntradaBlog
                .Include(b => b.Autor)
                .Select(b => new BlogResponse
                {
                    id = b.id,
                    titulo = b.titulo,
                    contenido = b.contenido,
                    fecha_creacion = b.fecha_creacion,
                    fecha_edicion = b.fecha_edicion,
                    estado = b.estado,
                    fk_Autor = b.fk_Autor,
                    autor = b.Autor.nombre ?? b.Autor.email
                })
                .ToList();
        }



        public BlogResponse? GetBlogById(int id)
        {

            var blog = _context.EntradaBlog
                .Include(b => b.Autor)
                .FirstOrDefault(x => x.id == id);


            if (blog == null)
                return null;


            return Map(blog);
        }


        public BlogResponse CrearBlog(CreateBlogRequest request)
        {

            var blog = new EntradaBlogModel
            {
                titulo = request.titulo,
                contenido = request.contenido,
                fk_Autor = request.fk_Autor,
                estado = true,
                fecha_creacion = DateTime.Now
            };


            _context.EntradaBlog.Add(blog);

            _context.SaveChanges();



            _auditoria.Registrar(
                "BLOG",
                "CREATE",
                $"Blog creado: {blog.titulo}",
                "admin"
            );


            return Map(blog);

        }



        public BlogResponse? ActualizarBlog(
            int id,
            UpdateBlogRequest request)
        {

            var blog = _context.EntradaBlog
                .FirstOrDefault(x => x.id == id);


            if (blog == null)
                return null;


            blog.titulo = request.titulo;
            blog.contenido = request.contenido;
            blog.estado = request.estado;
            blog.fecha_edicion = DateTime.Now;


            _context.SaveChanges();


            return Map(blog);

        }



        public BlogResponse? DesactivarBlog(int id)
        {

            var blog = _context.EntradaBlog
                .FirstOrDefault(x => x.id == id);


            if (blog == null)
                return null;


            blog.estado = false;
            blog.fecha_edicion = DateTime.Now;


            _context.SaveChanges();


            return Map(blog);

        }



        private static BlogResponse Map(
            EntradaBlogModel blog)
        {

            return new BlogResponse
            {
                id = blog.id,
                titulo = blog.titulo,
                contenido = blog.contenido,
                fecha_creacion = blog.fecha_creacion,
                fecha_edicion = blog.fecha_edicion,
                estado = blog.estado,
                fk_Autor = blog.fk_Autor,
                autor = blog.Autor?.nombre ?? blog.Autor?.email
            };

        }


    }
}