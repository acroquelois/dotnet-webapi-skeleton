﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Skeleton.Domain.Enum;
using Skeleton.Domain.Models.Users;
using Skeleton.Domain.Services.AuthServices;
using Skeleton.Domain.Services.AuthServices.Abstractions;
using Skeleton.Domain.Services.Core;

namespace Skeleton.Api.Controllers
{
    [Authorize]
    [Route("[controller]"), ApiController]
    public class AuthController : ControllerBase
    {

        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// </summary>
        /// <param name="userAuth"></param>
        /// <param name="authService"></param>
        /// <param name="boOptions"></param>
        /// <returns></returns>
        [AllowAnonymous, HttpPost("token")]
        public async Task<ActionResult<TokenResponse>> Token(
            [FromBody] UserAuthDto userAuth,
            [FromServices] IAuthService authService)
        {
            try
            {

                var (authError, token) = await authService.LoginAsync(new User { Login = userAuth.Mail, MotDePasse = userAuth.Password});

                if (authError == AuthError.EmptyUsername)
                {
                    return Unauthorized(new { Message = "Utilisateur ou mot de passe incorrect." });
                }

                if (authError == AuthError.Forbidden)
                {
                    return Unauthorized(new { Message = "Utilisateur ou mot de passe incorrect." });
                }

                return Ok(new TokenResponse { AccessToken = token });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { Message = "Erreur technique", Trace = e.StackTrace });
            }
        }
        

        [AllowAnonymous, HttpPost("resetpassword")]
        public async Task<ActionResult<TokenResponse>> ResetPasswordBo(
            [FromBody] UserAuthDto userAuth,
            [FromServices] IAuthService authService)
        {
            string resetToken = "";
            try
            {
                resetToken = await authService.CreateResetToken(userAuth.Mail);
                if (string.IsNullOrEmpty(resetToken))
                {
                    return Unauthorized(new { Message = "L'email n'existe pas" });
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, new { Message = "Erreur technique", Trace = e.StackTrace });
            }
            
            return NoContent();
        }


        [HttpPost("password")]
        public async Task<ActionResult<TokenResponse>> Password(
            [FromBody] UserAuthDto userAuth,
            [FromServices] UserService userService,
            [FromServices] IPasswordHasher<User> passwordHasher)
        {
            Claim claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid);

            if (claim == null)
            {
                return Unauthorized(new { Message = "token invalide" });
            }

            int id = int.Parse(claim.Value);
            User user = await userService.GetAsync(id);
            if (user == null)
            {
                return Unauthorized(new { Message = "token invalide" });
            }
            user.MotDePasse = passwordHasher.HashPassword(user, userAuth.Password);
            await userService.Update(user);

            return NoContent();
        }
    }

    public class UserAuthDto
    {
        public string Mail { get; set; }

        public string Password { get; set; }
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; }
    }
}