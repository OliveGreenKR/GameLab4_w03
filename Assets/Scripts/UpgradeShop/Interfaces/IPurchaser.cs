/// <summary>구매 자원을 가진 구매자 인터페이스</summary>
public interface IPurchaser
{
    /// <summary>지정된 비용을 지불할 수 있는지 확인</summary>
    /// <param name="cost">확인할 비용</param>
    /// <returns>지불 가능 여부</returns>
    bool CanAfford(int cost);

    /// <summary>지정된 비용을 지불</summary>
    /// <param name="cost">지불할 비용</param>
    /// <returns>지불 성공 여부</returns>
    bool SpendGold(int cost);
}