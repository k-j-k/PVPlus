패치내역

[v1.0.3]
 1. LTFHelper의 텍스트컬러가 의도한 대로 표시되지 않는 현상 수정 

[v1.0.5]
 1. GP0, GP6 검증 시 orgValue가 0일때 calValue를 1로 산출합니다<br>if ((checkResult.Key == "GP6" || checkResult.Key == "GP0") && checkResult.Value[0] == 0) ResultType = "오차";
