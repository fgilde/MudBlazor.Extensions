package crc6452ffdc5b34af3a0f;


public class NavigationRootManager_ElementBasedFragment
	extends crc6452ffdc5b34af3a0f.ScopedFragment
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onViewCreated:(Landroid/view/View;Landroid/os/Bundle;)V:GetOnViewCreated_Landroid_view_View_Landroid_os_Bundle_Handler\n" +
			"";
		mono.android.Runtime.register ("Microsoft.Maui.Platform.NavigationRootManager+ElementBasedFragment, Microsoft.Maui", NavigationRootManager_ElementBasedFragment.class, __md_methods);
	}


	public NavigationRootManager_ElementBasedFragment ()
	{
		super ();
		if (getClass () == NavigationRootManager_ElementBasedFragment.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Platform.NavigationRootManager+ElementBasedFragment, Microsoft.Maui", "", this, new java.lang.Object[] {  });
		}
	}


	public NavigationRootManager_ElementBasedFragment (int p0)
	{
		super (p0);
		if (getClass () == NavigationRootManager_ElementBasedFragment.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Platform.NavigationRootManager+ElementBasedFragment, Microsoft.Maui", "System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0 });
		}
	}


	public void onViewCreated (android.view.View p0, android.os.Bundle p1)
	{
		n_onViewCreated (p0, p1);
	}

	private native void n_onViewCreated (android.view.View p0, android.os.Bundle p1);

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
