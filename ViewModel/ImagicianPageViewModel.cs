﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExifLib;
using Xamarin.Forms;

namespace Imagician
{

	public class ImagicianPageViewModel : BaseViewModel
	{
		string _previousSelectedPath;
		IFileService _fileService;
		IImageService _imageService;
		IFolderService _folderService;
		ILogService _logService;
		ISettingsService _settingsService;
		Stack<FolderItem> _nav = new Stack<FolderItem>();


		public ImagicianPageViewModel()
		{
			_logService = DependencyService.Get<ILogService>();
			_fileService = DependencyService.Get<IFileService>();
			_imageService = DependencyService.Get<IImageService>();
			_folderService = DependencyService.Get<IFolderService>();
			_settingsService = DependencyService.Get<ISettingsService>();
			PropertyChanged += (sender, e) =>
			{

				if (e.PropertyName == "IsBusy")
					GetImagesCommand.ChangeCanExecute();
			};
		}

		public void Init()
		{
			SelectedPath = new FolderItem { Title = "root", Path = Path.Combine("/", "Volumes"), IsFolder = true };
		}

		bool _isRecursive = true;
		public bool IsRecursive
		{
			get { return _isRecursive; }
			private set { SetProperty(ref _isRecursive, value); }
		}

		ObservableCollection<FolderItem> _items = new ObservableCollection<FolderItem>();

		public ObservableCollection<FolderItem> Items
		{
			get { return _items; }
			private set { SetProperty(ref _items, value); }
		}


		FolderItem _selectedPath;
		public FolderItem SelectedPath
		{
			get { return _selectedPath; }
			set
			{
				if (_selectedPath == value)
					return;

				if (!value.IsFolder)
				{
					SelectedFile = value;
					return;
				}

				SetProperty(ref _selectedPath, value);
				GetFiles();
				if (_nav.Count == 0 || _nav.Peek() != _selectedPath)
					_nav.Push(_selectedPath);

				GoBackCommand.ChangeCanExecute();
				GetImagesCommand.ChangeCanExecute();
			}
		}

		FolderItem _selectedFile;
		public FolderItem SelectedFile
		{
			get { return _selectedFile; }
			set
			{
				if (_selectedFile == value)
					return;
				if (value.IsFolder)
					return;
				SetProperty(ref _selectedFile, value);
				if (!_selectedFile.IsImage)
					return;
				using (var stream = _fileService.GetFileStream(_selectedFile.ImagePath))
				{
					SelectedImageInfo = ExifReader.ReadJpeg(stream);
				}
			}
		}

		JpegInfo _selectedImageInfo;
		public JpegInfo SelectedImageInfo
		{
			get { return _selectedImageInfo; }
			set
			{
				if (_selectedImageInfo == value)
					return;

				SetProperty(ref _selectedImageInfo, value);

			}
		}

		public ObservableCollection<string> Messages
		{
			get { return _logService.Messages; }
		}

		string _backupPath;
		public string BackupPath
		{
			get { return _settingsService.GetSetting<string>(SettingsService.BackupPath); }
			set
			{
				if (_backupPath == value)
					return;
				SetProperty(ref _backupPath, value);
				_settingsService.SetSetting<string>(SettingsService.BackupPath, _backupPath);
			}
		}


		Command _goBackCommand;
		public Command GoBackCommand
		{
			get
			{
				return _goBackCommand ?? (_goBackCommand = new Command((arg) =>
				{
					var s = _nav.Pop();
					SelectedPath = _nav.Peek();

				}, (arg) => _nav.Count > 1));
			}
		}

		Command _getImagesCommand;
		public Command GetImagesCommand
		{
			get
			{
				return _getImagesCommand ?? (_getImagesCommand = new Command(async (arg) =>
				{
					IsBusy = true;
					var path = SelectedPath.Path;
					_logService.AddMessage(nameof(ImagicianPageViewModel), $"Start Parsing Folder {path}");
					await _imageService.ParseFolderForImagesAsync(path, IsRecursive);
					_logService.AddMessage(nameof(ImagicianPageViewModel), $"Ended Parsing Folder {path}");
					IsBusy = false;
				}, (arg) => SelectedPath != null && !IsBusy));
			}
		}

		void GetFiles()
		{
			Items.Clear();
			foreach (var item in _folderService.GetFilesForPath(SelectedPath.Path))
			{
				Items.Add(item);
			}
		}
	}
}
