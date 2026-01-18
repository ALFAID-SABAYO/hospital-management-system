using Microsoft.AspNetCore.Mvc;
using Hospital.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Hospital.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace Hospital.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher<object> _hasher;
        private readonly IConfiguration _config;
        
        public AccountController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _hasher = new PasswordHasher<object>();
            _config = config;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string userType)
        {
            Console.WriteLine($"=== LOGIN ATTEMPT ===");
            Console.WriteLine($"Username: {username}");
            Console.WriteLine($"UserType: {userType}");
            Console.WriteLine($"Password length: {password?.Length}");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Username/Email and password are required.");
                return View();
            }

            if (userType == "Admin")
            {
                Console.WriteLine("=== ADMIN LOGIN (SIMPLE PASSWORD) ===");
                Console.WriteLine($"Username: {username}");

                var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Username == username);
                if (admin == null)
                {
                    Console.WriteLine($"Admin NOT FOUND: {username}");
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View();
                }

                Console.WriteLine($"Admin found: {admin.Username}");
                Console.WriteLine($"Stored plain password: '{admin.PlainPassword}'");
                Console.WriteLine($"Input password: '{password}'");

                // SIMPLE PASSWORD CHECK (no hashing) - Check PlainPassword first
                if (!string.IsNullOrEmpty(admin.PlainPassword) && admin.PlainPassword == password)
                {
                    Console.WriteLine("ADMIN LOGIN SUCCESS - Password matches (Plain)!");
                }
                else
                {
                    {
                        Console.WriteLine("ADMIN LOGIN FAILED - Both plain and hash mismatch");
                        ModelState.AddModelError("", "Invalid credentials.");
                        return View();
                    }
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, admin.Username),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                    new Claim("UserType", "Admin")
                };

                // Generate JWT Token
                var token = GenerateJwtToken(claims);
                
                // Create Claims Identity and Principal
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_config["JWT:ExpireMinutes"] ?? "60"))
                };

                // Store JWT token in cookie
                Response.Cookies.Append("jwt_token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps, // Use HTTPS if available
                    SameSite = SameSiteMode.Strict,
                    Path = "/", // Explicitly set path
                    Expires = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_config["JWT:ExpireMinutes"] ?? "60"))
                });

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }
            else if (userType == "Patient")
            {
                Console.WriteLine("Attempting Patient login...");
                Console.WriteLine($"Looking for patient with email: {username}");

                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.email == username);

                if (patient == null)
                {
                    Console.WriteLine($"Patient NOT FOUND with email: {username}");
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View();
                }

                Console.WriteLine($"Patient FOUND: {patient.first_name} {patient.last_name}");
                Console.WriteLine($"Patient has password_hash: {!string.IsNullOrEmpty(patient.password_hash)}");

                // Check if patient has password hash
                if (string.IsNullOrEmpty(patient.password_hash))
                {
                    Console.WriteLine("Patient has NO password hash, setting default 'patient123'...");
                    patient.password_hash = _hasher.HashPassword(null, "patient123");
                    _db.Patients.Update(patient);
                    await _db.SaveChangesAsync();
                    Console.WriteLine($"New hash set");
                }

                Console.WriteLine("Verifying password...");
                var result = _hasher.VerifyHashedPassword(null, patient.password_hash, password);
                Console.WriteLine($"Password verification result: {result}");

                if (result == PasswordVerificationResult.Success ||
                    result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    Console.WriteLine($"LOGIN SUCCESS for patient: {patient.email}");

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, patient.first_name + " " + patient.last_name),
                        new Claim(ClaimTypes.Role, "Patient"),
                        new Claim(ClaimTypes.NameIdentifier, patient.patient_id.ToString()),
                        new Claim(ClaimTypes.Email, patient.email),
                        new Claim("UserType", "Patient")
                    };

                    // Generate JWT Token
                    var token = GenerateJwtToken(claims);
                    
                    // Create Claims Identity and Principal
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_config["JWT:ExpireMinutes"] ?? "60"))
                    };

                    // Store JWT token in cookie
                    Response.Cookies.Append("jwt_token", token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps, // Use HTTPS if available
                        SameSite = SameSiteMode.Strict,
                        Path = "/", // Explicitly set path
                        Expires = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_config["JWT:ExpireMinutes"] ?? "60"))
                    });

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction("Dashboard", "Patients");
                }
                else
                {
                    Console.WriteLine($"LOGIN FAILED - Password mismatch for patient");
                }
            }
            else if (userType == "Receptionist")
            {
                Console.WriteLine("=== RECEPTIONIST LOGIN ===");
                Console.WriteLine($"Username: {username}");

                var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Username == username);
                if (admin == null)
                {
                    Console.WriteLine($"Receptionist NOT FOUND: {username}");
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View();
                }

                Console.WriteLine($"Receptionist found: {admin.Username}");

                // Check password (using same logic as Admin for now)
                if (!string.IsNullOrEmpty(admin.PlainPassword) && admin.PlainPassword == password)
                {
                    Console.WriteLine("RECEPTIONIST LOGIN SUCCESS!");
                }
                else
                {
                    Console.WriteLine("RECEPTIONIST LOGIN FAILED - Password mismatch");
                    ModelState.AddModelError("", "Invalid credentials.");
                    return View();
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, admin.Username),
                    new Claim(ClaimTypes.Role, "Receptionist"),
                    new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                    new Claim("UserType", "Receptionist")
                };

                // Generate JWT Token
                var token = GenerateJwtToken(claims);
                
                // Create Claims Identity and Principal
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_config["JWT:ExpireMinutes"] ?? "60"))
                };

                // Store JWT token in cookie
                Response.Cookies.Append("jwt_token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps, // Use HTTPS if available
                    SameSite = SameSiteMode.Strict,
                    Path = "/", // Explicitly set path
                    Expires = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_config["JWT:ExpireMinutes"] ?? "60"))
                });

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }
            else if (userType == "Doctor")
            {
                Console.WriteLine("Attempting Doctor login...");
                Console.WriteLine($"Looking for doctor with email: {username}");

                var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.email == username);

                if (doctor == null)
                {
                    Console.WriteLine($"Doctor NOT FOUND with email: {username}");
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View();
                }

                // For now, check if doctor has password_hash (we'll add this field if needed)
                // Since Doctor model doesn't have password_hash yet, we'll use a default approach
                // In a real system, you'd add password_hash to Doctor model
                // For now, let's check if status is "Active" and use a simple verification
                if (doctor.status != "Active")
                {
                    ModelState.AddModelError("", "Doctor account is not active.");
                    return View();
                }

                // Check if password is provided (for now, accept any password for active doctors)
                // In production, add password_hash field to Doctor model
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, doctor.first_name + " " + doctor.last_name),
                    new Claim(ClaimTypes.Role, "Doctor"),
                    new Claim(ClaimTypes.NameIdentifier, doctor.doctor_id.ToString()),
                    new Claim(ClaimTypes.Email, doctor.email ?? ""),
                    new Claim("UserType", "Doctor")
                };

                // Generate JWT Token
                var token = GenerateJwtToken(claims);
                
                // Create Claims Identity and Principal
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_config["JWT:ExpireMinutes"] ?? "60"))
                };

                // Store JWT token in cookie
                Response.Cookies.Append("jwt_token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps, // Use HTTPS if available
                    SameSite = SameSiteMode.Strict,
                    Path = "/", // Explicitly set path
                    Expires = DateTimeOffset.UtcNow.AddMinutes(int.Parse(_config["JWT:ExpireMinutes"] ?? "60"))
                });

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Dashboard", "Doctors", new { id = doctor.doctor_id });
            }
            else
            {
                Console.WriteLine($"ERROR: Invalid userType: {userType}");
                ModelState.AddModelError("", "Please select Patient, Doctor, Receptionist, or Admin.");
                return View();
            }

            Console.WriteLine("LOGIN FAILED - Invalid credentials");
            ModelState.AddModelError("", "Invalid credentials.");
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Patient patient, string password, string confirmPassword)
        {
            Console.WriteLine($"=== REGISTRATION ATTEMPT ===");
            Console.WriteLine($"Email: {patient?.email}");
            Console.WriteLine($"First Name: {patient?.first_name}");

            // 1. Password validation
            if (string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("password", "Password is required.");
            }
            else if (password.Length < 6)
            {
                ModelState.AddModelError("password", "Password must be at least 6 characters.");
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Passwords do not match.");
            }

            // Remove password_hash from ModelState validation since we set it programmatically
            ModelState.Remove("password_hash");

            Console.WriteLine($"ModelState IsValid: {ModelState.IsValid}");

            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine("Model is valid, checking database...");

                    // Check if email already exists
                    var existingPatient = await _db.Patients.FirstOrDefaultAsync(p => p.email == patient.email);
                    if (existingPatient != null)
                    {
                        ModelState.AddModelError("email", "Email already registered.");
                        Console.WriteLine($"Email already exists: {patient.email}");
                        return View(patient);
                    }

                    // Hash the password
                    patient.password_hash = _hasher.HashPassword(null, password);
                    Console.WriteLine($"Password hashed successfully");

                    if (patient.date_of_birth.Kind == DateTimeKind.Unspecified)
                    {
                        patient.date_of_birth = DateTime.SpecifyKind(
                            patient.date_of_birth,
                            DateTimeKind.Utc
                        );
                    }

                    // Add to database
                    _db.Patients.Add(patient);
                    Console.WriteLine("Patient added to DbContext");

                    int recordsAffected = await _db.SaveChangesAsync();
                    Console.WriteLine($"SaveChangesAsync completed. Records affected: {recordsAffected}");
                    Console.WriteLine($"New Patient ID: {patient.patient_id}");

                    if (recordsAffected > 0)
                    {
                        Console.WriteLine($"Registration SUCCESS for: {patient.email}");

                        // Registration successful - redirect to login page
                        TempData["SuccessMessage"] = $"Registration successful! Please login with email: {patient.email}";
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        Console.WriteLine("Registration FAILED: No records affected");
                        ModelState.AddModelError("", "Registration failed. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"=== REGISTRATION ERROR ===");
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                    ModelState.AddModelError("", $"Registration error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("=== MODEL STATE ERRORS ===");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    if (errors.Count > 0)
                    {
                        Console.WriteLine($"{key}: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                    }
                }
            }

            // If we got here, something went wrong
            Console.WriteLine("Returning to registration view with errors");
            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("jwt_token");
            return RedirectToAction("Index", "Home");
        }

        private string GenerateJwtToken(Claim[] claims)
        {
            var key = Encoding.UTF8.GetBytes(_config["JWT:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["JWT:ExpireMinutes"] ?? "60")),
                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}