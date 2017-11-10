using UnityEngine;
using System.Collections.Generic;
using System.Threading;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
    public class DeckLinkAudioOutput : MonoBehaviour
    {
        private HashSet<int> _registeredDevices;
		private Mutex _mutex;

        void Awake()
        {
            _registeredDevices = new HashSet<int>();
			_mutex = new Mutex();

		}

        // Use this for initialization
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void RegisterDevice(int deviceIndex)
        {
			_mutex.WaitOne();
			// TODO: replace this Add as it (and .Contains()) generate 20 bytes of garbage per frame
			_registeredDevices.Add(deviceIndex);
			_mutex.ReleaseMutex();
		}

        public void UnregisterDevice(int deviceIndex)
        {
			_mutex.WaitOne();
			_registeredDevices.Remove(deviceIndex);
			_mutex.ReleaseMutex();
		}

        public void OnAudioFilterRead(float[] data, int channels)
        {
            DeckLinkManager manager = DeckLinkManager.Instance;
            if(manager == null)
            {
                return;
            }

			_mutex.WaitOne();
			foreach (var deviceIndex in _registeredDevices)
            {
                var device = manager.GetDevice(deviceIndex);

                if (device == null)
                {
					_mutex.ReleaseMutex();

					return;
                }

                short[] buffer = new short[data.Length];

                for (int i = 0; i < data.Length; ++i)
                {
                    buffer[i] = (short)(data[i] * 32767f);
                }

                DeckLinkPlugin.OutputAudio(deviceIndex, buffer, buffer.Length * 2);
            }
			_mutex.ReleaseMutex();
		}
    }
}

