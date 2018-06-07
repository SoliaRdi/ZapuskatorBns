using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using DeployLX.CodeVeil.CompileTime.v5;
using MahApps.Metro.Controls.Dialogs;
using Zapuskator.Framework;

namespace Zapuskator.ViewModels
{
    
    public class ManageProfilesViewModel:Screen
    {
        private readonly IEventAggregator _eventAggregator;
        public IDialogCoordinator _dialogCoordinator;
        public ManageProfilesViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _dialogCoordinator = DialogCoordinator.Instance;
        }

        public void AddProfile()
        {
            Services.Profiles.AllProfiles.Add(new AppSettings(){Name="Новый профиль"});
        }
        public async void DeleteProfile(AppSettings settings)
        {
            if (Services.Profiles.AllProfiles.Count == 1|| (Services.Profiles.LastUsedProfile==Services.Profiles.AllProfiles.IndexOf(settings)))
            {
                await _dialogCoordinator.ShowMessageAsync(this, "Ошибка", "Нельзя удальть единственный или активный профиль");
            }
            else
            {
                var result = await _dialogCoordinator.ShowMessageAsync(this, "Подтверждение", "Удалить?", MessageDialogStyle.AffirmativeAndNegative);
                if (result == MessageDialogResult.Affirmative)
                {
                    Services.Profiles.AllProfiles.Remove(settings);
                    Services.Profiles.CurrentProfile = Services.Profiles.AllProfiles.First();
                }
            }
            
        }
    }
}