using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
	public enum DuplexMode { FULL = 0, HALF };

	[System.Serializable]
	public struct DeviceSetting
	{
		public string deviceName;
		public bool single;
		public int deviceIndex;

		public bool setDuplexMode;
		public DuplexMode duplexMode;
	}

	[AddComponentMenu("AVPro DeckLink/DeckLinkSettings")]
	public class DeckLinkSettings : Singleton<DeckLinkSettings>
	{
		[HideInInspector]
		public List<DeviceSetting> _deviceSettings;
		[HideInInspector]
		public bool _showSettings = false;
		
		public bool _multiOutput = false;

		private List<Device> findDevices(int deviceIndex, string deviceName, bool single)
		{
			var filtered = new List<Device>();
			int numDevices = DeckLinkManager.Instance.NumDevices;

			if (numDevices < 1)
			{
				return filtered;
			}

			for (int i = 0; i < numDevices; ++i)
			{
				filtered.Add(DeckLinkManager.Instance.GetDevice(i));
			}

			filtered = filtered.Where(x => x.Name.Contains(deviceName)).ToList();

			if(single)
			{
				deviceIndex = deviceIndex >= 0 ? deviceIndex : 0;

				if(deviceIndex > filtered.Count)
				{
					filtered.Clear();
				}
				else
				{
					Device temp = filtered[deviceIndex];
					filtered.Clear();
					filtered.Add(temp);
				}
			}

			return filtered;
		}

		// Use this for initialization
		void Start()
		{
			if(_deviceSettings == null)
			{
				return;
			}

			foreach(var setting in _deviceSettings)
			{
				List<Device> foundDevices = findDevices(setting.deviceIndex, setting.deviceName, setting.single);

				foreach(Device device in foundDevices)
				{
					if (setting.setDuplexMode)
					{
						bool isFull = setting.duplexMode == DuplexMode.FULL;
						device.SetDuplexMode(isFull);
					}
				}
			}

			DeckLinkManager.Instance.Reset();
		}
	}
}

