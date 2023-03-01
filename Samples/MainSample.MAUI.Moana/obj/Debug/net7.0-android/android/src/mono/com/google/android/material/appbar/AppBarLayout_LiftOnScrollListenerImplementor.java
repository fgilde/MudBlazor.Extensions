package mono.com.google.android.material.appbar;


public class AppBarLayout_LiftOnScrollListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.google.android.material.appbar.AppBarLayout.LiftOnScrollListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onUpdate:(FI)V:GetOnUpdate_FIHandler:Google.Android.Material.AppBar.AppBarLayout/ILiftOnScrollListenerInvoker, Xamarin.Google.Android.Material\n" +
			"";
		mono.android.Runtime.register ("Google.Android.Material.AppBar.AppBarLayout+ILiftOnScrollListenerImplementor, Xamarin.Google.Android.Material", AppBarLayout_LiftOnScrollListenerImplementor.class, __md_methods);
	}


	public AppBarLayout_LiftOnScrollListenerImplementor ()
	{
		super ();
		if (getClass () == AppBarLayout_LiftOnScrollListenerImplementor.class) {
			mono.android.TypeManager.Activate ("Google.Android.Material.AppBar.AppBarLayout+ILiftOnScrollListenerImplementor, Xamarin.Google.Android.Material", "", this, new java.lang.Object[] {  });
		}
	}


	public void onUpdate (float p0, int p1)
	{
		n_onUpdate (p0, p1);
	}

	private native void n_onUpdate (float p0, int p1);

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
