﻿using origami_backend.Models;
using origami_backend.Utilities;
using origami_backend.Utilities.JWTUtilis;
using BCryptNet = BCrypt.Net.BCrypt;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using origami_backend.Repositories;
using origami_backend.Models.DTOs;

namespace origami_backend.Services
{
    public class UserService : IUserService
    {
        public IUserRepository _userRepository;
        public IProfileRepository _profileRepository;
        public IProfileCommentRepository _profileCommentRepository;
        private IJWTUtils _JWtUtils;

        public UserService(
            IUserRepository userRepository,
            IProfileRepository profileRepository,
            IProfileCommentRepository profileCommentRepository,
            IJWTUtils JWtUtils)
        {
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _profileCommentRepository = profileCommentRepository;
            _JWtUtils = JWtUtils;
        }

        public LoginResponseDTO Authenticate(LoginRequestDTO req)
        {
            var user = _userRepository.GetByUsername(req.Username);

            if(user == null || !BCryptNet.Verify(req.Password, user.PasswordHash))
            {
                return null;
            }

            var jwtToken = _JWtUtils.GenerateToken(user);
            return new LoginResponseDTO(user, jwtToken);
        }

        public LoginResponseDTO Register(RegisterRequestDTO req)
        {
            var user = _userRepository.GetByUsername(req.Username);
            if (user != null)
            {
                return null;
            }

            var profile = new Profile
            {
                FirstName = req.FirstName,
                LastName = req.LastName,
                Birthday = req.Birthday
            };

            _profileRepository.Create(profile);
            
            bool success = _profileRepository.Save();
            
            if (!success)
            {
                return null;
            }

            user = new User
            {
                Username = req.Username,
                PasswordHash = BCryptNet.HashPassword(req.Password),
                HashSalt = BCryptNet.GenerateSalt(),
                Email = req.Email,
                Role = Role.User,
                ProfileId = profile.Id
            };

            _userRepository.Create(user);
            success = _userRepository.Save();
            
            if (!success)
            {
                return null;
            }

            var jwtToken = _JWtUtils.GenerateToken(user);
            return new LoginResponseDTO(user, jwtToken);
        }

        public User GetByUsername(string username)
        {
            return _userRepository.GetByUsername(username);
        }

        public User Get(Guid id)
        {
            return _userRepository.Get(id);
        }

        public ProfileDTO GetProfileDTO(string username)
        {
            var user = _userRepository.GetByUsernameIncludingProfile(username);
            if (user == null)
            {
                return null;
            }

            return new ProfileDTO(user);
        }

        public CommentDTO PostComment(CommentDTO comment)
        {
            var profileUser = _userRepository.GetByUsername(comment.ProfileUsername);
            if (profileUser == null)
            {
                return null;
            }

            var commenterUser = _userRepository.GetByUsername(comment.CommenterUsername);
            if (commenterUser == null)
            {
                return null;
            }

            var profileComment = new ProfileComment
            {
                UserId = commenterUser.Id,
                ProfileId = profileUser.ProfileId,
                Body = comment.Body
            };

            _profileCommentRepository.Create(profileComment);
            bool success = _profileCommentRepository.Save();

            if (!success)
            {
                return null;
            }
            comment.DateUpdated = profileComment.DateUpdated;
            return comment;
        }

        public CommentDTO UpdateComment(CommentDTO comment)
        {
            var profileUser = _userRepository.GetByUsername(comment.ProfileUsername);
            if (profileUser == null)
            {
                return null;
            }

            var commenterUser = _userRepository.GetByUsername(comment.CommenterUsername);
            if (commenterUser == null)
            {
                return null;
            }

            var profileComment = _profileCommentRepository.Get(comment.Id);
            if (profileComment == null || !profileComment.UserId.Equals(commenterUser.Id))
            {
                return null;
            }

            profileComment.Body = comment.Body;
            
            _profileCommentRepository.Update(profileComment);
            bool success = _profileCommentRepository.Save();

            if (!success)
            {
                return null;
            }
            comment.DateUpdated = profileComment.DateUpdated;
            return comment;
        }

        public CommentDTO DeleteComment(CommentDTO commentDTO)
        {
            var comment = _profileCommentRepository.Get(commentDTO.Id);
            if (comment == null)
            {
                return commentDTO;
            }

            _profileCommentRepository.Delete(comment);
            bool success = _profileCommentRepository.Save();

            if (!success)
            {
                return null;
            }

            return commentDTO;
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _userRepository.GetAll().ToList();
        }
    }
}
