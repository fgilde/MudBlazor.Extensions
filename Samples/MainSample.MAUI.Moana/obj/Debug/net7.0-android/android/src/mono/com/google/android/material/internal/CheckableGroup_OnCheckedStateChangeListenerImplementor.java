package mono.com.google.android.material.internal;


public class CheckableGroup_OnCheckedStateChangeListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.google.android.material.internal.CheckableGroup.OnCheckedStateChangeListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onCheckedStateChanged:(Ljava/util/Set;)V:GetOnCheckedStateChanged_Ljava_util_Set_Handler:Google.Android.Material.Internal.CheckableGroup/IOnCheckedStateChangeListenerInvoker, Xamarin.Google.Android.Material\n" +
			"";
		mono.android.Runtime.register ("Google.Android.Material.Internal.CheckableGroup+IOnCheckedStateChangeListenerImplementor, Xamarin.Google.Android.Material", CheckableGroup_OnCheckedStateChangeListenerImplementor.class, __md_methods);
	}


	public CheckableGroup_OnCheckedStateChangeListenerImplementor ()
	{
		super ();
		if (getClass () == CheckableGroup_OnCheckedStateChangeListenerImplementor.class) {
			mono.android.TypeManager.Activate ("Google.Android.Material.Internal.CheckableGroup+IOnCheckedStateChangeListenerImplementor, Xamarin.Google.Android.Material", "", this, new java.lang.Object[] {  });
		}
	}


	public void onCheckedStateChanged (java.util.Set p0)
	{
		n_onCheckedStateChanged (p0);
	}

	private native void n_onCheckedStateChanged (java.util.Set p0);

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
