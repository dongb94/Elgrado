
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.SocialPlatforms;

public class RankingPanel : MonoBehaviour
{
    public RankingData[] RankingDataGroup;
    public float UpperLimit;
    public float LowerLimit;
    
    private Camera _uiCamera;
    private Vector3 _mousePosition;
    private bool _isRankPanelEvent;

    private float _rankingDataSpriteHeight;

    private int curruntIndex;

    private static IScore[] _rankingBuffer;
    private int _bufferSize;
    
    #region <Enum>

    public enum RankSort
    {
        TopBased,
        PlayerBased
    }    

    #endregion
    
    private void Awake()
    {
        _uiCamera = FindObjectOfType<UICamera>().GetComponent<Camera>();
        _rankingDataSpriteHeight = 80f;
        _bufferSize = 30;
        _rankingBuffer = new IScore[_bufferSize];
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown((int)MouseButton.LeftMouse)) OnMouseEnter();
        if (_isRankPanelEvent && Input.GetMouseButton((int) MouseButton.LeftMouse)) OnMouseDrag();
        else _isRankPanelEvent = false;
    }

    public void OnMouseEnter()
    {
        _mousePosition = Input.mousePosition;
        RaycastHit hit;
        Physics.Raycast(_uiCamera.ScreenPointToRay(_mousePosition), out hit);
        if (hit.collider != null && hit.collider.gameObject == gameObject) _isRankPanelEvent = true;
    }
    
    public void OnMouseDrag()
    {
        var position = Input.mousePosition;
        var variation = (position.y - _mousePosition.y);
        foreach (var rankingData in RankingDataGroup)
        {
            if (rankingData.transform.localPosition.y + variation > UpperLimit)
            {
                var nextRank = curruntIndex + RankingDataGroup.Length;
                if (nextRank >= _rankingBuffer.Length || _rankingBuffer[nextRank] == null) break;// TODO 끝 부분 처리
            
                rankingData.transform.localPosition -= Vector3.up * _rankingDataSpriteHeight * RankingDataGroup.Length;
                rankingData.RankLabel.text = _rankingBuffer[nextRank].rank.ToString();
                rankingData.NameLabel.text = _rankingBuffer[nextRank].userID;
                rankingData.ScoreLabel.text = _rankingBuffer[nextRank].value.ToString();
                
                curruntIndex++;
            
                
            }else if (rankingData.transform.localPosition.y + variation < LowerLimit)
            {
                var nextRank = curruntIndex - 1;
                if (nextRank < 0 || _rankingBuffer[nextRank] == null) break;
                
                rankingData.transform.localPosition += Vector3.up * _rankingDataSpriteHeight * RankingDataGroup.Length;
                rankingData.RankLabel.text = _rankingBuffer[nextRank].rank.ToString();
                rankingData.NameLabel.text = _rankingBuffer[nextRank].userID;
                rankingData.ScoreLabel.text = _rankingBuffer[nextRank].value.ToString();
                
                curruntIndex--;
                
            }
            rankingData.transform.localPosition += Vector3.up * variation;
        }
        _mousePosition = position;
    }
    
    public void LeaderBoardSet(RankSort sort)
    {
        switch (sort)
        {
//            case RankSort.TopBased:
//                PlayGamesPlatform.Instance.LoadScores(GPGSIds.getLeaderBoard(1),//temp
//                    LeaderboardStart.TopScores, _bufferSize, LeaderboardCollection.Public, LeaderboardTimeSpan.AllTime,
//                    data =>
//                    {
//                        for (var i = 0; i < data.Scores.Length; i++)
//                        {
//                            _rankingBuffer[i] = data.Scores[i];
//                        }
//                    });
//                break;
//            case RankSort.PlayerBased:
//                PlayGamesPlatform.Instance.LoadScores(GPGSIds.getLeaderBoard(1),//temp
//                    LeaderboardStart.PlayerCentered, _bufferSize, LeaderboardCollection.Public, LeaderboardTimeSpan.AllTime,
//                    data =>
//                    {
//                        for (var i = 0; i < data.Scores.Length; i++)
//                        {
//                            _rankingBuffer[i] = data.Scores[i];
//                        }
//                    });
//                break;
        }
        
        curruntIndex = 0;
        var index = 0;
        foreach (var rankingData in RankingDataGroup)
        {
            if (_rankingBuffer[index] == null)
            {
                rankingData.gameObject.SetActive(false);
                continue;
            }
            var datas = _rankingBuffer[index];
            rankingData.Rank = datas.rank;
            rankingData.RankLabel.text = datas.rank.ToString();
            rankingData.NameLabel.text = datas.userID;
            rankingData.ScoreLabel.text = datas.value.ToString();
            index++;
        }
    }

}
