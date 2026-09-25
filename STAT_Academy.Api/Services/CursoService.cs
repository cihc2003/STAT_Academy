using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.DTOs.Curso;

namespace STAT_Academy.Api.Services
{
    public class CursoService
    {
        private readonly ApplicationDbContext _context;

        public CursoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CursoModel> GetAll()
        {
            return _context.Curso
                .OrderBy(x => x.nombre)
                .ToList();
        }

        public CursoModel GetById(int id)
        {
            return _context.Curso.FirstOrDefault(x => x.id == id);
        }

        public CursoModel Crear(CursoCreateRequest request)
        {
            CursoModel curso = new CursoModel();

            curso.fk_tutor = request.fk_tutor;
            curso.fk_creador = request.fk_creador;
            curso.nombre = request.nombre;
            curso.descripcion = request.descripcion;
            curso.precio = request.precio;
            curso.duracionSemanas = request.duracionSemanas;
            curso.fechaInicio = request.fechaInicio;
            curso.fechaFin = request.fechaFin;
            curso.estado = true;
            curso.fechaCreacion = DateTime.Now;
            curso.fechaEdicion = DateTime.Now;

            _context.Curso.Add(curso);
            _context.SaveChanges();

            return curso;
        }

        public CursoModel Editar(int id, CursoCreateRequest request)
        {
            var curso = _context.Curso.FirstOrDefault(x => x.id == id);

            if (curso == null)
                return null;

            curso.fk_tutor = request.fk_tutor;
            curso.fk_creador = request.fk_creador;
            curso.nombre = request.nombre;
            curso.descripcion = request.descripcion;
            curso.precio = request.precio;
            curso.duracionSemanas = request.duracionSemanas;
            curso.fechaInicio = request.fechaInicio;
            curso.fechaFin = request.fechaFin;
            curso.fechaEdicion = DateTime.Now;

            _context.SaveChanges();

            return curso;
        }

        public CursoModel Activar(int id)
        {
            var curso = _context.Curso.FirstOrDefault(x => x.id == id);

            if (curso == null)
                return null;

            curso.estado = true;
            curso.fechaEdicion = DateTime.Now;

            _context.SaveChanges();

            return curso;
        }

        public CursoModel Desactivar(int id)
        {
            var curso = _context.Curso.FirstOrDefault(x => x.id == id);

            if (curso == null)
                return null;

            curso.estado = false;
            curso.fechaEdicion = DateTime.Now;

            _context.SaveChanges();

            return curso;
        }
    }
}