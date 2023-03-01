package crc6488302ad6e9e4df1a;


public class ImageLoaderResultCallback
	extends crc6488302ad6e9e4df1a.ImageLoaderCallbackBase_1
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("Microsoft.Maui.ImageLoaderResultCallback, Microsoft.Maui", ImageLoaderResultCallback.class, __md_methods);
	}


	public ImageLoaderResultCallback ()
	{
		super ();
		if (getClass () == ImageLoaderResultCallback.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.ImageLoaderResultCallback, Microsoft.Maui", "", this, new java.lang.Object[] {  });
		}
	}

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
