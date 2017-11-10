using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
	[RequireComponent(typeof(TextMesh))]
	public class TextMeshFrameCounter : MonoBehaviour 
	{
		private TextMesh _text;

		void Start()
		{
			_text = this.GetComponent<TextMesh>();
		}
		
		void Update()
		{
			if (_text != null)
			{
				_text.text = Time.frameCount.ToString();
			}
		}
	}
}