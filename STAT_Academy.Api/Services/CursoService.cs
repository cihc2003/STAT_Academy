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

        public List<CursoModel> GetAll(bool incluirInactivos)
        {
            return _context.Curso
                .Where(x => incluirInactivos || x.estado)
                .OrderBy(x => x.nombre)
                .ToList();
        }

        public CursoModel GetById(int id)
        {
            return _context.Curso.FirstOrDefault(x => x.id == id);
        }

        /// <summary>
        /// El tutor asignado debe ser un usuario activo con rol Tutor (FK_Tipo_Usuario = 2);
        /// si no, el curso no aparecería en ningún panel de tutor.
        /// </summary>
        public string? ValidarTutor(int fkTutor)
        {
            var tutor = _context.Usuario
                .Where(u => u.id == fkTutor)
                .Select(u => new { u.fk_Tipo_Usuario, u.estado })
                .FirstOrDefault();

            if (tutor == null)
                return $"No existe un usuario con id {fkTutor}.";

            if (tutor.fk_Tipo_Usuario != 2)
                return $"El usuario {fkTutor} no tiene rol de tutor.";

            if (!tutor.estado)
                return $"El tutor {fkTutor} está inactivo.";

            return null;
        }

        public CursoModel Crear(CursoCreateRequest request)
        {
            CursoModel curso = new CursoModel();

            curso.fk_tutor = request.fk_tutor;
            curso.fk_creador = request.fk_creador;
            curso.nombre = request.nombre;
            curso.descripcion = request.descripcion;
            curso.precio = request.precio;
            curso.fechaInicio = request.fechaInicio;
            curso.fechaFin = request.fechaFin;
            curso.estado = true;

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
            curso.fechaInicio = request.fechaInicio;
            curso.fechaFin = request.fechaFin;

            _context.SaveChanges();

            return curso;
        }

        public CursoModel Activar(int id)
        {
            var curso = _context.Curso.FirstOrDefault(x => x.id == id);

            if (curso == null)
                return null;

            curso.estado = true;

            _context.SaveChanges();

            return curso;
        }

        public CursoModel Desactivar(int id)
        {
            var curso = _context.Curso.FirstOrDefault(x => x.id == id);

            if (curso == null)
                return null;

            curso.estado = false;

            _context.SaveChanges();

            return curso;
        }
    }
}