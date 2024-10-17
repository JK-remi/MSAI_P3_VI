using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum eNotice
{
    CREATE_OVER,
    CREATE_FILE_COPYRIGHT,
    MODIFY_DELETE,
    MODIFY_FILE_COPYRIGHT,
    ETC_SUGGEST,

    FILE_SIZE_OVER,
}

public class NoticeInfo
{
    public class Notice
    {
        public string title;
        public string desc;
        public string btn;

        public Notice(string title, string desc, string btn)
        {
            this.title = title;
            this.desc = desc;
            this.btn = btn;
        }
    }

    public static Notice CREATE_OVER = new Notice( "생성 가능한 캐릭터 갯수를 초과했습니다.",
                                                    "캐릭터는 최대 6개까지 생성 가능합니다.",
                                                    "확인");
    public static Notice CREATE_COPYRIGHT = new Notice("저작권은 무서운거에요 돈이걸렸거든요...",
                                                    "타인의 저작권을 침해하는 행위는 지양해주세요.\n\n1. 기존에 판매되는 소설, 웹소설등에서 추출한 텍스트 파일은 사용하지 말아주세요\n2. 유료, 또는 무료로 발행되는 타인의 창작물을 그대로 사용하지 말아주세요.\n3. 저작권 침해로 인한 피해 발생 시 사용자에게 책임소재가 있음을 안내드립니다.",
                                                    "확인했습니다.");
    public static Notice MODIFY_DELETE = new Notice("삭제가 완료된 캐릭터는 복구가 불가합니다.\n삭제하시겠습니까?",
                                                    "",
                                                    "삭제하기");
    public static Notice MODIFY_FILE = new Notice("새로운 파일 업로드 시 기존에 업로드된 파일은 삭제됩니다.",
                                                    "타인의 저작권을 침해하는 행위는 지양해주세요.\n\n1. 기존에 판매되는 소설, 웹소설등에서 추출한 텍스트 파일은 사용하지 말아주세요\n2. 유료, 또는 무료로 발행되는 타인의 창작물을 그대로 사용하지 말아주세요.\n3. 저작권 침해로 인한 피해 발생 시 사용자에게 책임소재가 있음을 안내드립니다.",
                                                    "확인");
    public static Notice ETC_SUGGEST = new Notice("제출이 완료되었습니다!\n의견에 감사드립니다.",
                                                    "",
                                                    "확인");
    public static Notice FILE_SIZE_OVER = new Notice("업로드 가능한 파일 크기를 초과하였습니다.",
                                                    "업로드 가능한 파일 크기는 최대 10MB입니다.",
                                                    "");
}
