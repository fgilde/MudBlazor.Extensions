package crc645d80431ce5f73f11;


public class ReorderableItemsViewAdapter_2
	extends crc645d80431ce5f73f11.GroupableItemsViewAdapter_2
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("Microsoft.Maui.Controls.Handlers.Items.ReorderableItemsViewAdapter`2, Microsoft.Maui.Controls", ReorderableItemsViewAdapter_2.class, __md_methods);
	}


	public ReorderableItemsViewAdapter_2 ()
	{
		super ();
		if (getClass () == ReorderableItemsViewAdapter_2.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Controls.Handlers.Items.ReorderableItemsViewAdapter`2, Microsoft.Maui.Controls", "", this, new java.lang.Object[] {  });
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
