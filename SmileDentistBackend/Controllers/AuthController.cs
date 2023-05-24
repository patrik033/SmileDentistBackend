using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SmileDentistBackend.Data;
using SmileDentistBackend.Email.Registering;
using SmileDentistBackend.Email.Token;
using SmileDentistBackend.Models.Dto;
using SmileDentistBackend.Models;
using SmileDentistBackend.Utility;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace SmileDentistBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly QuartzContext _context;
        private ApiResponse _response;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;
        private readonly ISendGridEmailTokens _tokenSender;
        private readonly ISendGridEmailRegister _tokenEmail;
        private readonly ILogger<AuthController> _logger;
        private readonly string secretKey;

        public AuthController(QuartzContext context,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IMapper mapper,
            ISendGridEmailTokens tokenSender,
            ISendGridEmailRegister tokenEmail,
            ILogger<AuthController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _tokenSender = tokenSender;
            _tokenEmail = tokenEmail;
            _logger = logger;
            secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _response = new ApiResponse();
        }

        [Authorize]
        [HttpPost("updatePassword")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequestDTO password)
        {
            if (password.UserId == null || password.OldPassword == null || password.RepeatedPassword == null || password.NewPassword == null)
            {
                ResponseResult(HttpStatusCode.BadRequest, false, "Some fields were missed", "");
                return BadRequest(_response);
            }
            if (password.NewPassword != password.RepeatedPassword)
            {
                ResponseResult(HttpStatusCode.BadRequest, false, "New and repeated passwords do not match", "");
                return BadRequest(_response);
            }
            //var user = _userManager.FindByIdAsync(password.UserId);
            var applicationUser = _context.ApplicationUsers.Where(x => x.Id == password.UserId).FirstOrDefault();
            if (applicationUser == null)
            {
                ResponseResult(HttpStatusCode.BadRequest, false, "User was not found", "");
                return NotFound(_response);
            }
            else
            {
                var setPassword = await _userManager.ChangePasswordAsync(applicationUser, password.OldPassword, password.NewPassword);
                if (setPassword.Succeeded)
                {
                    var token = await RefreshAuthToken(applicationUser);
                    ResponseResult(HttpStatusCode.OK, true, "", token);
                    return Ok(_response);
                }
                else
                {
                    ResponseResult(HttpStatusCode.InternalServerError, false, "The server was unable to update the password", "");
                    return StatusCode(StatusCodes.Status500InternalServerError, _response);
                }
            }
        }

        [Authorize]
        [HttpPost("updateEmail")]
        public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequestDTO email)
        {
            if (email.NewEmail != null || email.UserId != null)
            {
                var applicationUser = _context.ApplicationUsers.Where(x => x.Id == email.UserId).FirstOrDefault();
                if (applicationUser != null)
                {
                    var setEmail = await _userManager.SetEmailAsync(applicationUser, email.NewEmail);
                    applicationUser.UserName = email.NewEmail;
                    applicationUser.NormalizedEmail = email.NewEmail.ToUpper();
                    var save = await _context.SaveChangesAsync();
                    if (setEmail.Succeeded)
                    {
                        string token = await RefreshAuthToken(applicationUser);
                        ResponseResult(HttpStatusCode.OK, true, "", token);
                        return Ok(_response);
                    }
                    else
                    {
                        ResponseResult(HttpStatusCode.Conflict, false, "Unable to update email", "");
                        return Conflict(_response);
                    }
                }
                ResponseResult(HttpStatusCode.NotFound, false, $"No user with id: {email.UserId} was found", "");
                return NotFound(_response);
            }
            ResponseResult(HttpStatusCode.BadRequest, false, "One or more fields were missing", "");
            return BadRequest(_response);
        }

        [Authorize]
        [HttpPost("updateName")]
        public async Task<IActionResult> UpdateName([FromBody] UpdateNameRequestDTO name)
        {
            if (name.NewName != null || name.OldName != null || name.UserId != null)
            {
                var applicationUser = _context.ApplicationUsers.Where(x => x.Id == name.UserId).FirstOrDefault();
                if (applicationUser != null)
                {
                    applicationUser.Name = name.NewName;
                    var save = await _context.SaveChangesAsync();
                    if (save == 1)
                    {
                        //we have to generate jwt token
                        string Token = await RefreshAuthToken(applicationUser);
                        //set response
                        ResponseResult(HttpStatusCode.OK, true, "", Token);
                        return Ok(_response);
                    }
                    else
                    {
                        ResponseResult(HttpStatusCode.Conflict, false, "Unable to update the same item to the database, use a unique name", "");
                        return Conflict(_response);
                    }
                }
                ResponseResult(HttpStatusCode.NotFound, false, $"No user with id: {name.UserId} was found", "");
                return NotFound(_response);
            }
            ResponseResult(HttpStatusCode.BadRequest, false, "One or more fields were missing!", "");
            return BadRequest(_response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginRequestDTO)
        {
            var user = _context.ApplicationUsers.FirstOrDefault(x => x.UserName.ToLower() == loginRequestDTO.UserName.ToLower());
            //check to see if the correct username and password was entered
            bool isValid = await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password);
            //return a invalid response
            if (isValid == false)
            {
                ResponseResult(HttpStatusCode.Unauthorized, false, "Username or password was incorrect", new LoginResponseDTO());
                return StatusCode(StatusCodes.Status401Unauthorized, _response);
            }
            bool isConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            if (isConfirmed == false)
            {
                ResponseResult(HttpStatusCode.Forbidden, false, "You must confirm your account before you login!", "");
                return StatusCode(StatusCodes.Status403Forbidden, _response);
            }

            //we have to generate jwt token
            JwtSecurityTokenHandler tokenHandler = new();
            var roles = await _userManager.GetRolesAsync(user);
            byte[] key = Encoding.ASCII.GetBytes(secretKey);


            //add claims to the token
            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("fullName", user.Name),
                    new Claim("id", user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.UserName.ToString()),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                //set signin credentials
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };

            //here the actual token is generated:
            //populates the token with the claims
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);


            LoginResponseDTO loginResponse = new()
            {
                Email = user.Email,
                Token = tokenHandler.WriteToken(token),
            };

            //if no email was provided or token is null/empty
            if (loginResponse.Email == null || string.IsNullOrEmpty(loginResponse.Token))
            {
                ResponseResult(HttpStatusCode.BadRequest, false, "Username or password is incorrect", "");
                return BadRequest(_response);
            }
            ResponseResult(HttpStatusCode.OK, true, "", loginResponse);
            return Ok(_response);
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string activationToken)
        {
            if (email.IsNullOrEmpty() || activationToken.IsNullOrEmpty())
            {
                ResponseResult(HttpStatusCode.BadRequest, false, "Either email or verification code is invalid.", "");
                return BadRequest(_response);
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, Base64UrlEncoder.Decode(activationToken));

                if (result.Succeeded)
                {
                    ResponseResult(HttpStatusCode.OK, true, "", result);
                    return Ok(_response);
                }
                else
                {
                    var identityDescribor = new IdentityErrorDescriber().InvalidToken();
                    var some = result.Errors.Any(x => x.Code == nameof(IdentityErrorDescriber.InvalidToken));
                    if (some)
                    {
                        ResponseResult(HttpStatusCode.Forbidden, false, "The token has expired", "");
                        return StatusCode(StatusCodes.Status401Unauthorized, _response);
                    }
                }
            }
            ResponseResult(HttpStatusCode.InternalServerError, false, "An error occurred while confirmation your email address.", "");
            return StatusCode(StatusCodes.Status500InternalServerError, _response);
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO registerRequestDTO)
        {

            if (registerRequestDTO == null || !ModelState.IsValid)
                return BadRequest();

            if (registerRequestDTO.Role == null)
            {
                registerRequestDTO.Role = StaticDetails.Role_User;
            }

            //check if a user exists with the same username as you tried to create
            var user = _context.ApplicationUsers.FirstOrDefault(x => x.UserName.ToLower() == registerRequestDTO.UserName.ToLower());

            //create a new response if the user exists
            if (user != null)
            {
                ResponseResult(HttpStatusCode.BadRequest, false, "Username already exists", "");
                return BadRequest(_response);
            }
            else
            {
                //if no user with that username => creates a new object of that user and then creates it in the database
                var notExistingUser = new ApplicationUser()
                {
                    UserName = registerRequestDTO.UserName,
                    Email = registerRequestDTO.UserName,
                    NormalizedEmail = registerRequestDTO.UserName.ToUpper(),
                    Name = registerRequestDTO.Name,
                };

                try
                {
                    var result = await _userManager.CreateAsync(notExistingUser, registerRequestDTO.Password);

                    if (result.Succeeded)
                    {
                        if (!_roleManager.RoleExistsAsync(StaticDetails.Role_Admin).GetAwaiter().GetResult())
                        {
                            //create roles in database
                            await _roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_Admin));
                            await _roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_User));
                        }
                        if (registerRequestDTO.Role.ToLower() == StaticDetails.Role_Admin.ToLower())
                        {
                            await _userManager.AddToRoleAsync(notExistingUser, StaticDetails.Role_Admin);
                        }
                        else
                        {
                            await _userManager.AddToRoleAsync(notExistingUser, StaticDetails.Role_User);
                        }

                        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(notExistingUser);
                        var urlToken = Base64UrlEncoder.Encode(emailToken);
                        string link = string.Format($"http://localhost:3000/RegisterConfirmation?email={notExistingUser.Email}&activationToken={urlToken}");
                        string body = $"<a href='{link}'>Confirm</a>";
                        await _tokenEmail.SendAsync("patrik.odh@gmail.com", "patrik.odh@gmail.com", "Confirm email link", body);

                        ResponseResult(HttpStatusCode.OK, true, "", "");
                        return Ok(_response);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An exception was thrown: {ex.Message}", ex);
                }
                ResponseResult(HttpStatusCode.BadRequest, false, "Error while registering", "");
                return BadRequest(_response);
            }
        }

        [HttpPost("resend")]
        public async Task<IActionResult> ResendEmailActivationLink([FromBody] ResendRequestAgain email)
        {
            var user = await _userManager.FindByEmailAsync(email.Email);
            if (user != null)
            {

                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                //var confirmLink = Url.p(nameof(ConfirmEmail), "Account", new { emailToken, email = notExistingUser.Email }, Request.Scheme);
                var urlToken = Base64UrlEncoder.Encode(emailToken);
                string link = string.Format($"http://localhost:3000/RegisterConfirmation?email={user.Email}&activationToken={urlToken}");
                string body = $"<a href='{link}'>Confirm</a>";

                await _tokenEmail.SendAsync("patrik.odh@gmail.com", "patrik.odh@gmail.com", "Confirm email link", body);

                ResponseResult(HttpStatusCode.OK, true, "", "");
                return Ok(_response);
            }
            ResponseResult(HttpStatusCode.Unauthorized, false, "Invalid User", "");
            return StatusCode(StatusCodes.Status401Unauthorized, _response);
        }

        [HttpPost("resetpassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO forgotPassword)
        {
            var user = await _userManager.FindByEmailAsync(forgotPassword.Email);
            if (user != null)
            {
                var passwordToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                string link = $"http://localhost:3000/UpdatePassword?email={user.Email}&activationToken={Base64UrlEncoder.Encode(passwordToken)}";
                string body = $"<a href='{link}'>Confirm</a>";

                await _tokenSender.SendAsync("patrik.odh@gmail.com", "patrik.odh@gmail.com", "Reset Password link", body, link, user.Name);
                ResponseResult(HttpStatusCode.OK, true, "Password reset was successfull", "");
                return Ok(_response);
            }
            ResponseResult(HttpStatusCode.Unauthorized, false, "", "");
            return BadRequest(_response);
        }

        [AllowAnonymous]
        [HttpGet("confirmpasswordreset")]
        public async Task<IActionResult> ConfirmPasswordReset([FromQuery] string email, [FromQuery] string password, [FromQuery] string activationToken)
        {
            if (email.IsNullOrEmpty() || password.IsNullOrEmpty() || activationToken.IsNullOrEmpty())
            {
                ResponseResult(HttpStatusCode.BadRequest, false, "confirmed and new password doesn't match or email is empty", "");
                return BadRequest(_response);
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var result = await _userManager.ResetPasswordAsync(user, Base64UrlEncoder.Decode(activationToken), password);

                if (result.Succeeded)
                {
                    ResponseResult(HttpStatusCode.OK, true, "", result);
                    return Ok(_response);
                }
                else
                {
                    var identityDescribor = new IdentityErrorDescriber().InvalidToken();
                    var some = result.Errors.Any(x => x.Code == nameof(IdentityErrorDescriber.InvalidToken));
                    if (some)
                    {
                        ResponseResult(HttpStatusCode.Forbidden, false, "The token has expired", "");
                        return StatusCode(StatusCodes.Status401Unauthorized, _response);
                    }
                }
            }
            ResponseResult(HttpStatusCode.InternalServerError, false, "An error occurred while confirmation your email address.", "");
            return StatusCode(StatusCodes.Status500InternalServerError, _response);
        }

        private void ResponseResult(HttpStatusCode statusCode, bool successResult, string errorMessage, object apiResult)
        {
            _response.StatusCode = statusCode;
            _response.IsSuccess = successResult;
            _response.ErrorMessages.Add(errorMessage);
            _response.Result = apiResult;
        }
        private async Task<string> RefreshAuthToken(ApplicationUser? applicationUser)
        {
            JwtSecurityTokenHandler tokenHandler = new();
            var roles = await _userManager.GetRolesAsync(applicationUser);
            byte[] key = Encoding.ASCII.GetBytes(secretKey);

            //add claims to the token
            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                                new Claim("fullName", applicationUser.Name),
                                new Claim("id", applicationUser.Id.ToString()),
                                new Claim(ClaimTypes.Email, applicationUser.UserName.ToString()),
                                new Claim(ClaimTypes.Role, roles.FirstOrDefault())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                //set signin credentials
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };

            //here the actual token is generated:
            //populates the token with the claims
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

            var Token = tokenHandler.WriteToken(token);
            return Token;
        }
    }
}
