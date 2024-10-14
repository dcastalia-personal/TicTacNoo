using UnityEngine;
using UnityEngine.UIElements;

public static class UIExt
{
	public static async Awaitable SwitchStyleOnFirstFrame( this VisualElement element, string oldStyle, string newStyle ) {
		await Awaitable.NextFrameAsync();

		if( oldStyle != "" ) element.RemoveFromClassList( oldStyle );
		element.AddToClassList( newStyle );
	}
}
