package crc6488302ad6e9e4df1a;


public class ImageLoaderCallback
	extends crc6488302ad6e9e4df1a.ImageLoaderCallbackBase_1
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("Microsoft.Maui.ImageLoaderCallback, Microsoft.Maui", ImageLoaderCallback.class, __md_methods);
	}


	public ImageLoaderCallback ()
	{
		super ();
		if (getClass () == ImageLoaderCallback.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.ImageLoaderCallback, Microsoft.Maui", "", this, new java.lang.Object[] {  });
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
