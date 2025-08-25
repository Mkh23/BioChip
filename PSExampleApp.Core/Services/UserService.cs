﻿using PSExampleApp.Common.Models;
using PSExampleApp.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PSExampleApp.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private User _activeUser;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public event EventHandler<User> ActiveUserChanged;

        public User ActiveUser
        {
            get => _activeUser;
            private set
            {
                _activeUser = value;
                ActiveUserChanged?.Invoke(this, value);
            }
        }

        public async Task DeleteMeasurementInfo(Guid id)
        {
            var infoToDelete = this.ActiveUser.Measurements.FirstOrDefault(x => x.Id == id);

            if (infoToDelete == null)
                return;

            this.ActiveUser.Measurements.Remove(infoToDelete);
            await _userRepository.UpdateUser(ActiveUser);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }

        public async Task<User> LoadUserAsync(Guid id)
        {
            var loadedUser = await _userRepository.LoadUserById(id);
            ActiveUser = loadedUser;
            return loadedUser;
        }

        public async Task SaveMeasurementInfo(HeavyMetalMeasurement measurement)
        {
            if (!this.ActiveUser.Measurements.Any(x => x.Name == measurement.Name))
                this.ActiveUser.Measurements.Add(new MeasurementInfo { Id = measurement.Id, Name = measurement.Name, MeasurementDate = DateTime.Now });

            await _userRepository.UpdateUser(ActiveUser);
        }

        public async Task SaveUserAsync(string username)
        {
            var user = new User { Name = username, Password = "123", Id = Guid.NewGuid(), IsAdmin = false };
            await _userRepository.UpdateUser(user);

            ActiveUser = user;
        }

        public void SetActiveUser(User user)
        {
            ActiveUser = user;
        }

        public async Task UpdateUserSettings(Language language)
        {
            await _userRepository.UpdateUser(ActiveUser);
            this.ActiveUserChanged.Invoke(this, ActiveUser);
        }

        public async Task DeleteUserAsync(Guid id)
        {
            if (ActiveUser.Id != id)
                await _userRepository.DeleteUserAsync(id);
        }
    }
}