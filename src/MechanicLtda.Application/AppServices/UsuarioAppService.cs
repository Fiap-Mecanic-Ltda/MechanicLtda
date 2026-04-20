using AutoMapper;
using MechanicLtda.Application.AppServices.Interfaces;
using MechanicLtda.Application.DTOs;
using MechanicLtda.Domain.Entities;
using MechanicLtda.Domain.Interfaces.Services;

namespace MechanicLtda.Application.AppServices
{
    public class UsuarioAppService : IUsuarioAppService
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IMapper _mapper;

        public UsuarioAppService(IUsuarioService usuarioService, IMapper mapper)
        {
            _usuarioService = usuarioService;
            _mapper = mapper;
        }

        public async Task<ResponseDto<UsuarioDto>> AdicionarAsync(UsuarioCreateDto dto)
        {
            var response = new ResponseDto<UsuarioDto>();

            try
            {
                var usuario = await _usuarioService.AdicionarAsnyc(dto.UserName, dto.Email, dto.Tipo);

                return response.setResponse(_mapper.Map<UsuarioDto>(usuario));
            }
            catch(Exception ex)
            {
                return response.addError(ex);
            }
        }

        public async Task<ResponseDto<UsuarioDto>> AtualizarAsync(string id, UsuarioUpdateDto dto)
        {
            var response = new ResponseDto<UsuarioDto>();

            try
            {
                var usuario = _mapper.Map<Usuario>(dto);
                usuario.Id = id;
                var resultado = await _usuarioService.AtualizarAsync(usuario);

                return response.setResponse(_mapper.Map<UsuarioDto>(resultado));
            }
            catch (Exception ex)
            {
                return response.addError(ex);
            }
        }

        public async Task<ResponseDto<IEnumerable<UsuarioDto>>> ObterTodosAsync()
        {
            var response = new ResponseDto<IEnumerable<UsuarioDto>>();
            try
            {
                var usuarios = await _usuarioService.ObterTodosAsync();
                return response.setResponse(_mapper.Map<IEnumerable<UsuarioDto>>(usuarios));
            }
            catch (Exception ex) { return response.addError(ex); }
        }

        public async Task<ResponseDto<bool>> RemoverAsync(string id)
        {
            var response = new ResponseDto<bool>();
            try
            {
                await _usuarioService.RemoverAsync(id);
                return response.setResponse(true);
            }
            catch (KeyNotFoundException ex) { return response.addError(ex.Message); }
            catch (Exception ex) { return response.addError(ex); }
        }
    }
}
