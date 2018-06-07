using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using DeployLX.CodeVeil.CompileTime.v5;
using MahApps.Metro.Controls.Dialogs;
using Zapuskator.Framework;
using Zapuskator.lib;

namespace Zapuskator.ViewModels
{
    
    public class XmlEditorViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;
        public IDialogCoordinator _dialogCoordinator;

        public XmlEditorViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _dialogCoordinator = DialogCoordinator.Instance;
        }

        public async void ApplyXml()
        {
            try
            {
                var foldername = Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\Local\INNOVA\data\";
                bool tIs64 = Services.Profiles.CurrentProfile.GeneralSettings.BnsPriority != 0;
                string filename = foldername + (tIs64 ? "xml64.dat" : "xml.dat");
                string filenameConfig = foldername + (tIs64 ? "config64.dat" : "config.dat");
                if (File.Exists(filename))
                {
                    if (tIs64)
                        _eventAggregator.PublishOnUIThread("Применение патча xml для версии x64.");
                    else
                        _eventAggregator.PublishOnUIThread("Применение патча xml для версии x32.");
                    var controller = await _dialogCoordinator.ShowProgressAsync(this, "Применение изменений.", "");
                    controller.SetIndeterminate();
                    await Task.Factory.StartNew(() => { new BNSDat().Extract(filename, (number, of) => { controller.SetMessage("Распаковка xml файлов: " + number + "/" + of); }, tIs64); });
                    await Task.Factory.StartNew(() => { new BNSDat().Extract(filenameConfig, (number, of) => { controller.SetMessage("Распаковка config файлов: " + number + "/" + of); }, tIs64); });

                    string input = File.ReadAllText(filename + ".files\\client.config2.xml");
                    string inputSystem = File.ReadAllText(filenameConfig + ".files\\system.config2.xml");
                    string inputWarlock = File.ReadAllText(filename + ".files\\skill3_contextscriptdata_warlock.xml");
                    if (Services.Profiles.CurrentProfile.XmlSettings.WarlockLich)
                    {
                        if (inputWarlock.Substring(0, inputWarlock.IndexOf("28011")).Contains("28220"))
                        {
                            inputWarlock = inputWarlock.Replace("28011", "lich").Replace("28220", "shipi").Replace("lich", "28220").Replace("shipi", "28011");
                        }
                    }
                    else
                    {
                        if (inputWarlock.Substring(0, inputWarlock.IndexOf("28220")).Contains("28011"))
                        {
                            inputWarlock = inputWarlock.Replace("28011", "lich").Replace("28220", "shipi").Replace("lich", "28220").Replace("shipi", "28011");
                        }
                    }
                    if (Services.Profiles.CurrentProfile.XmlSettings.DpsMod)
                        input = input.Replace("<option name=\"show-effect-only-info\" value=\"n\" />", "<option name=\"show-effect-only-info\" value=\"y\" />")
                            .Replace("<option name=\"showtype-public-zone\" value=\"0\" />", "<option name=\"showtype-public-zone\" value=\"2\" />")
                            .Replace("<option name=\"showtype-party-6-dungeon-and-cave\" value=\"1\" />", "<option name=\"showtype-party-6-dungeon-and-cave\" value=\"2\" />")
                            .Replace("<option name=\"showtype-field-zone\" value=\"0\" />", "<option name=\"showtype-field-zone\" value=\"2\" />")
                            .Replace("<option name=\"showtype-classic-field-zone\" value=\"0\" />", "<option name=\"showtype-classic-field-zone\" value=\"2\" />")
                            .Replace("<option name=\"showtype-faction-battle-field-zone\" value=\"0\" />", "<option name=\"showtype-faction-battle-field-zone\" value=\"2\" />")
                            .Replace("<option name=\"showtype-jackpot-boss-zone\" value=\"0\" />", "<option name=\"showtype-jackpot-boss-zone\" value=\"2\" />");
                    else
                        input = input.Replace("<option name=\"show-effect-only-info\" value=\"y\" />", "<option name=\"show-effect-only-info\" value=\"n\" />")
                            .Replace("<option name=\"showtype-public-zone\" value=\"2\" />", "<option name=\"showtype-public-zone\" value=\"0\" />")
                            .Replace("<option name=\"showtype-party-6-dungeon-and-cave\" value=\"2\" />", "<option name=\"showtype-party-6-dungeon-and-cave\" value=\"1\" />")
                            .Replace("<option name=\"showtype-field-zone\" value=\"2\" />", "<option name=\"showtype-field-zone\" value=\"0\" />")
                            .Replace("<option name=\"showtype-classic-field-zone\" value=\"2\" />", "<option name=\"showtype-classic-field-zone\" value=\"0\" />")
                            .Replace("<option name=\"showtype-faction-battle-field-zone\" value=\"2\" />", "<option name=\"showtype-faction-battle-field-zone\" value=\"0\" />")
                            .Replace("<option name=\"showtype-jackpot-boss-zone\" value=\"2\" />", "<option name=\"showtype-jackpot-boss-zone\" value=\"0\" />");
                    if (Services.Profiles.CurrentProfile.XmlSettings.RatingMod)
                        input = input.Replace("<option name=\"use-team-average-score\" value=\"false\" />", "<option name=\"use-team-average-score\" value=\"true\" />");
                    else
                        input = input.Replace("<option name=\"use-team-average-score\" value=\"true\" />", "<option name=\"use-team-average-score\" value=\"false\" />");

                    if (Services.Profiles.CurrentProfile.XmlSettings.FastSkillChange)
                        input = input.Replace("<option name=\"train-complete-delay-time\" value=\"1.500000\" />", "<option name=\"train-complete-delay-time\" value=\"0.100000\" />");
                    else
                        input = input.Replace("<option name=\"train-complete-delay-time\" value=\"0.100000\" />", "<option name=\"train-complete-delay-time\" value=\"1.500000\" />");
                    if (Services.Profiles.CurrentProfile.XmlSettings.FastItems)
                        input = input.Replace("<option name=\"pending-time\" value=\"0.300000\" />", "<option name=\"pending-time\" value=\"0.010000\" />")
                            .Replace("<option name=\"rapid-decompose-duration\" value=\"0.300000\" />", "<option name=\"rapid-decompose-duration\" value=\"0.001000\" />")
                            .Replace("<option name=\"self-restraint-gauge-time\" value=\"2.000000\" />", "<option name=\"self-restraint-gauge-time\" value=\"0.0500000\" />");
                    else
                        input = input.Replace("<option name=\"pending-time\" value=\"0.010000\" />", "<option name=\"pending-time\" value=\"0.300000\" />")
                            .Replace("<option name=\"rapid-decompose-duration\" value=\"0.001000\" />", "<option name=\"rapid-decompose-duration\" value=\"0.300000\" />")
                            .Replace("<option name=\"self-restraint-gauge-time\" value=\"0.0500000\" />", "<option name=\"self-restraint-gauge-time\" value=\"2.000000\" />");
                    if (Services.Profiles.CurrentProfile.XmlSettings.FastRes)
                        input = input.Replace("<option name=\"dying-postproc-rate\" value=\"30.000000\" />", "<option name=\"dying-postproc-rate\" value=\"1.000000\" />")
                            .Replace("<option name=\"delay-postproc-time\" value=\"3.000000\" />", "<option name=\"delay-postproc-time\" value=\"0.010000\" />")
                            .Replace("<option name=\"postproc-fade-in-time\" value=\"2.000000\" />", "<option name=\"postproc-fade-in-time\" value=\"0.010000\" />")
                            .Replace("<option name=\"postproc-fade-out-time\" value=\"2.000000\" />", "<option name=\"postproc-fade-out-time\" value=\"0.010000\" />")
                            .Replace("<option name=\"delay-ui-time\" value=\"5.000000\" />", "<option name=\"delay-ui-time\" value=\"0.010000\" />")
                            .Replace("<option name=\"exhaust-context-delay\" value=\"4.000000\" />", "<option name=\"exhaust-context-delay\" value=\"1.000000\" />")
                            .Replace("<option name=\"restoration-move-block\" value=\"2.000000\" />", "<option name=\"restoration-move-block\" value=\"0.001000\" />")
                            .Replace("<option name=\"instantaneous-dead-context-delay\" value=\"5\" />", "<option name=\"instantaneous-dead-context-delay\" value=\"3\" />")
                            .Replace("<option name=\"instantaneous-dead-context-delay\" value=\"0.1\" />", "<option name=\"instantaneous-dead-context-delay\" value=\"3\" />");//до патча
                    else
                        input = input.Replace("<option name=\"dying-postproc-rate\" value=\"1.000000\" />", "<option name=\"dying-postproc-rate\" value=\"30.000000\" />")
                            .Replace("<option name=\"delay-postproc-time\" value=\"0.010000\" />", "<option name=\"delay-postproc-time\" value=\"3.000000\" />")
                            .Replace("<option name=\"postproc-fade-in-time\" value=\"0.010000\" />", "<option name=\"postproc-fade-in-time\" value=\"2.000000\" />")
                            .Replace("<option name=\"postproc-fade-out-time\" value=\"0.010000\" />", "<option name=\"postproc-fade-out-time\" value=\"2.000000\" />")
                            .Replace("<option name=\"delay-ui-time\" value=\"0.010000\" />", "<option name=\"delay-ui-time\" value=\"5.000000\" />")
                            .Replace("<option name=\"exhaust-context-delay\" value=\"1.000000\" />", "<option name=\"exhaust-context-delay\" value=\"4.000000\" />")
                            .Replace("<option name=\"restoration-move-block\" value=\"0.001000\" />", "<option name=\"restoration-move-block\" value=\"2.000000\" />")
                            .Replace("<option name=\"instantaneous-dead-context-delay\" value=\"0.1\" />","<option name=\"instantaneous-dead-context-delay\" value=\"5\" />")
                            .Replace("<option name=\"instantaneous-dead-context-delay\" value=\"3\" />", "<option name=\"instantaneous-dead-context-delay\" value=\"5\" />");
                    if (Services.Profiles.CurrentProfile.XmlSettings.InviseIgnore)
                        input = input.Replace("<option name=\"other-hide-show-1\" value=\"00007916.hide_enemy5\" />", "<option name=\"other-hide-show-1\" value=\"00007916.hide\" />")
                            .Replace("<option name=\"other-hide-show-2\" value=\"00007916.hide_enemy4\" />", "<option name=\"other-hide-show-2\" value=\"00007916.hide\" />")
                            .Replace("<option name=\"other-hide-show-3\" value=\"00007916.hide_enemy3\" />", "<option name=\"other-hide-show-3\" value=\"00007916.hide\" />")
                            .Replace("<option name=\"other-hide-show-4\" value=\"00007916.hide_enemy2\" />", "<option name=\"other-hide-show-4\" value=\"00007916.hide\" />")
                            .Replace("<option name=\"other-hide-show-5\" value=\"00007916.hide_enemy1\" />", "<option name=\"other-hide-show-5\" value=\"00007916.hide\" />");
                    else
                        input = input.Replace("<option name=\"other-hide-show-1\" value=\"00007916.hide\" />", "<option name=\"other-hide-show-1\" value=\"00007916.hide_enemy5\" />")
                            .Replace("<option name=\"other-hide-show-2\" value=\"00007916.hide\" />", "<option name=\"other-hide-show-2\" value=\"00007916.hide_enemy4\" />")
                            .Replace("<option name=\"other-hide-show-3\" value=\"00007916.hide\" />", "<option name=\"other-hide-show-3\" value=\"00007916.hide_enemy3\" />")
                            .Replace("<option name=\"other-hide-show-4\" value=\"00007916.hide\" />", "<option name=\"other-hide-show-4\" value=\"00007916.hide_enemy2\" />")
                            .Replace("<option name=\"other-hide-show-5\" value=\"00007916.hide\" />", "<option name=\"other-hide-show-5\" value=\"00007916.hide_enemy1\" />");

                    if (Services.Profiles.CurrentProfile.XmlSettings.AfkMaster)
                    { 
                        input = input.Replace("<option name=\"limit-time-for-user-away-status\" value=\"600.000000\" />", "<option name=\"limit-time-for-user-away-status\" value=\"0\" />") ;
                        inputSystem = inputSystem.Replace("<option name=\"limit-time-for-user-away-status\" value=\"3600\" />", "<option name=\"limit-time-for-user-away-status\" value=\"0\" />") ;
                    }
                    else
                    {
                        input = input.Replace("<option name=\"limit-time-for-user-away-status\" value=\"0\" />", "<option name=\"limit-time-for-user-away-status\" value=\"600.000000\" />");
                        inputSystem = inputSystem.Replace("<option name=\"limit-time-for-user-away-status\" value=\"0\" />", "<option name=\"limit-time-for-user-away-status\" value=\"3600\" />");
                    }

                    if (Services.Profiles.CurrentProfile.XmlSettings.FastClose)
                        input = input.Replace("<option name=\"exit-game-waiting-time\" value=\"10.000000\" />", "<option name=\"exit-game-waiting-time\" value=\"1.000000\" />");
                    else
                        input = input.Replace("<option name=\"exit-game-waiting-time\" value=\"1.000000\" />", "<option name=\"exit-game-waiting-time\" value=\"10.000000\" />");

                    if (Services.Profiles.CurrentProfile.XmlSettings.NoLobbyAnim)
                        input = input.Replace("<option name=\"lobby-enter-ani-show\" value=\"true\" />", "<option name=\"lobby-enter-ani-show\" value=\"false\" />");
                    else
                        input = input.Replace("<option name=\"lobby-enter-ani-show\" value=\"false\" />", "<option name=\"lobby-enter-ani-show\" value=\"true\" />");

                    if (Services.Profiles.CurrentProfile.XmlSettings.OptimizedMode)
                        input = input.Replace("<option name=\"use-optimal-performance-mode-option\" value=\"false\" />", "<option name=\"use-optimal-performance-mode-option\" value=\"true\" />");
                    else
                        input = input.Replace("<option name=\"use-optimal-performance-mode-option\" value=\"true\" />", "<option name=\"use-optimal-performance-mode-option\" value=\"false\" />");

                    //if (Services.Profiles.CurrentProfile.XmlSettings.NoBoast)
                    //{
                    //    inputSystem = inputSystem.Replace("<option name=\"show-boast-npc-kill-timer\" value=\"true\" />", "<option name=\"show-boast-npc-kill-timer\" value=\"false\" />")
                    //        .Replace("<option name=\"use-boast-event\" value=\"true\" />", "<option name=\"use-boast-event\" value=\"false\" />");
                    //}
                    //else
                    //{
                    //    inputSystem = inputSystem.Replace("<option name=\"show-boast-npc-kill-timer\" value=\"false\" />", "<option name=\"show-boast-npc-kill-timer\" value=\"true\" />")
                    //        .Replace("<option name=\"use-boast-event\" value=\"false\" />", "<option name=\"use-boast-event\" value=\"true\" />");
                    //}

                    if (Services.Profiles.CurrentProfile.XmlSettings.NoTooltips)
                        input = input.Replace("<option name=\"loading-tip-refresh-cycle-duration\" value=\"7\" />", "<option name=\"loading-tip-refresh-cycle-duration\" value=\"0\" />");
                    else
                        input = input.Replace("<option name=\"loading-tip-refresh-cycle-duration\" value=\"0\" />", "<option name=\"loading-tip-refresh-cycle-duration\" value=\"7\" />");

                    File.WriteAllText(filename + ".files\\client.config2.xml", input);
                    File.WriteAllText(filenameConfig + ".files\\system.config2.xml", inputSystem);
                    File.WriteAllText(filename + ".files\\skill3_contextscriptdata_warlock.xml", inputWarlock);
                    await Task.Factory.StartNew(() => { new BNSDat().Compress(filename + ".files", (number, of) => { controller.SetMessage("Сжатие xml файлов: " + number + "/" + of); }, tIs64); });
                    await Task.Factory.StartNew(() => { new BNSDat().Compress(filenameConfig + ".files", (number, of) => { controller.SetMessage("Сжатие config файлов: " + number + "/" + of); }, tIs64); });
                    await controller.CloseAsync();
                    if (tIs64)
                        _eventAggregator.PublishOnUIThread("Xml изменен для версии x64.");
                    else
                        _eventAggregator.PublishOnUIThread("Xml изменен для версии x32.");
                }
                else
                {
                    _eventAggregator.PublishOnUIThread("Ошибка, файл xml не найден.");
                }
            }
            catch (Exception ex)
            {
                _eventAggregator.PublishOnUIThread(ex.Message);
            }
        }
    }
}