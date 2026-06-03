using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAT_Academy.Api.Data;    
using STAT_Academy.Api.Models;

namespace STAT_Academy.Api.Services

{
    public class UsuarioService
    {
        private readonly ApplicationDbContext _context;

        public UsuarioService(ApplicationDbContext context)
        {
            _context = context;
        }

        private readonly List<UsuarioModel> _usuario = new List<UsuarioModel>();
        private int _nextId = 1;

        public List<UsuarioModel> GetAll()
        {
            return _context.Usuario.ToList();
        }

        public List<UsuarioModel> FiltrarPorTipo(int tipo)
        {
            return _context.Usuario
                .Where(u => u.fk_Tipo_Usuario == tipo)
                .ToList();
        }

        public List<UsuarioModel> UsuariosActivos()
        {
            return _context.Usuario
                .Where(u => u.estado == true)
                .ToList();
        }

    }
}
