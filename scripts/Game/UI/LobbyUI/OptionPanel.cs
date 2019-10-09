
using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionPanel : Singleton<OptionPanel>
{
    public UISprite BgmButton;
    public UISprite SfxButton;

    private const string ButtonOff = "---";
    private const string ButtonOn = "Star_On";
    
    private bool _isBgmOff;
    private bool _isSfxOff;

    private LobbyMusicPlayer _player;

    protected override void Initialize()
    {
        _isBgmOff = PlayerPrefs.GetInt("BGM")==1;
        _isSfxOff = PlayerPrefs.GetInt("SFX")==1;
        
        BgmButton.spriteName = _isBgmOff ? ButtonOff : ButtonOn;
        SfxButton.spriteName = _isSfxOff ? ButtonOff : ButtonOn;
        
        _player = FindObjectOfType<LobbyMusicPlayer>();
    }

    public void PlayAd()
    {
        AdPlayer.GetInstance.PlayAd();
    }
    
    public void SetBGM()
    {
        _isBgmOff = !_isBgmOff;
        var code = _isBgmOff ? 1 : 0;
        PlayerPrefs.SetInt("BGM",code);
        RhythmManager.GetInstance.IsBgmOff = _isBgmOff;
        
        if(!_isBgmOff) _player.Play();
        else _player.Pause();

        BgmButton.spriteName = _isBgmOff ? ButtonOff : ButtonOn;
    }

    public void SetSFX()
    {
        _isSfxOff = !_isSfxOff;
        var code = _isSfxOff ? 1 : 0;
        PlayerPrefs.SetInt("SFX",code);
        SoundManager.GetInstance.IsSfxOff = _isSfxOff;
        
        SfxButton.spriteName = _isSfxOff ? ButtonOff : ButtonOn;
    }

    public void SignOut()
    {
        Login.Logout();
        SceneManager.LoadScene(0);
    }
}