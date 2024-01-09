<h2>패치내역</h2> 

<b>[v1.0.3]</b>
 1. LTFHelper의 텍스트컬러가 의도한 대로 표시되지 않는 현상 수정 

<b>[v1.0.5]</b>
 1. GP0, GP6 검증 시 orgValue가 0일때 calValue를 1로 산출합니다<br> <span style="color:blue">if ((checkResult.Key == "GP6" || checkResult.Key == "GP0") && checkResult.Value[0] == 0) ResultType = "오차";</span> <br> 영업보험료가 0으로 산출되는 것은 이상건으로 확인이 필요합니다.
 2. LTFHelper에서 입력 및 출력 인코딩을 UTF-8로 통일하였습니다.
