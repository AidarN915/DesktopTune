using DesktopTune.Model;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DesktopTune.ViewModel
{
    public  class CommandViewModel : BaseViewModel
    {
        private ObservableCollection<TwitchChatCommand> _chatCommands;
        private TwitchChatCommand _newCommand = new TwitchChatCommand();

        public CommandViewModel()
        {
            _chatCommands = new ObservableCollection<TwitchChatCommand>(SettingsService.LoadCommands());
            AddCommand = new RelayCommand(AddNewCommand);
            DeleteCommand = new RelayCommand<TwitchChatCommand>(DeleteSelectedCommand);
        }
        public ObservableCollection<TwitchChatCommand> Commands
        {
            get => _chatCommands;
        }
        public TwitchChatCommand NewCommand
        {
            get => _newCommand;
            set
            {
                _newCommand.Command = value.Command;
                _newCommand.Answer = value.Answer;
                OnPropertyChanged();
            }
        }

        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }


        private void AddNewCommand()
        {
            if (!string.IsNullOrWhiteSpace(NewCommand.Command) &&
                !string.IsNullOrWhiteSpace(NewCommand.Answer))
            {
                _chatCommands.Add(new TwitchChatCommand
                {
                    Command = NewCommand.Command,
                    Answer = NewCommand.Answer
                });

                NewCommand = new TwitchChatCommand(); // очистка формы
            }
            SettingsService.SaveCommands(new List<TwitchChatCommand>(_chatCommands));
        }

        private void DeleteSelectedCommand(TwitchChatCommand command)
        {
            if (command != null)
            {
                _chatCommands.Remove(command);
            }
            SettingsService.SaveCommands(new List<TwitchChatCommand>(_chatCommands));
        }
    }
}
