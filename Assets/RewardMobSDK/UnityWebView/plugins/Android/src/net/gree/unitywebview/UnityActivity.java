
import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import com.unity3d.player.UnityPlayer;

class UnityActivity extends Activity {
	
	@Override
	public void onCreate(Bundle data) {
		//Map<String, String> map = new HashMap<String, String>();
		//map.put("token", getIntent().getStringExtra("token");
		//map.put("expiration", getIntent().getIntExtra("expiration");

		UnityPlayer.UnitySendMessage("RewardMobGameObject", "ReceiveToken", true);
	}
}