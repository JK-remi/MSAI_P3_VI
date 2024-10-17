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

    public static Notice CREATE_OVER = new Notice( "���� ������ ĳ���� ������ �ʰ��߽��ϴ�.",
                                                    "ĳ���ʹ� �ִ� 6������ ���� �����մϴ�.",
                                                    "Ȯ��");
    public static Notice CREATE_COPYRIGHT = new Notice("���۱��� ������ſ��� ���̰ɷȰŵ��...",
                                                    "Ÿ���� ���۱��� ħ���ϴ� ������ �������ּ���.\n\n1. ������ �ǸŵǴ� �Ҽ�, ���Ҽ���� ������ �ؽ�Ʈ ������ ������� �����ּ���\n2. ����, �Ǵ� ����� ����Ǵ� Ÿ���� â�۹��� �״�� ������� �����ּ���.\n3. ���۱� ħ�ط� ���� ���� �߻� �� ����ڿ��� å�Ӽ��簡 ������ �ȳ��帳�ϴ�.",
                                                    "Ȯ���߽��ϴ�.");
    public static Notice MODIFY_DELETE = new Notice("������ �Ϸ�� ĳ���ʹ� ������ �Ұ��մϴ�.\n�����Ͻðڽ��ϱ�?",
                                                    "",
                                                    "�����ϱ�");
    public static Notice MODIFY_FILE = new Notice("���ο� ���� ���ε� �� ������ ���ε�� ������ �����˴ϴ�.",
                                                    "Ÿ���� ���۱��� ħ���ϴ� ������ �������ּ���.\n\n1. ������ �ǸŵǴ� �Ҽ�, ���Ҽ���� ������ �ؽ�Ʈ ������ ������� �����ּ���\n2. ����, �Ǵ� ����� ����Ǵ� Ÿ���� â�۹��� �״�� ������� �����ּ���.\n3. ���۱� ħ�ط� ���� ���� �߻� �� ����ڿ��� å�Ӽ��簡 ������ �ȳ��帳�ϴ�.",
                                                    "Ȯ��");
    public static Notice ETC_SUGGEST = new Notice("������ �Ϸ�Ǿ����ϴ�!\n�ǰ߿� ����帳�ϴ�.",
                                                    "",
                                                    "Ȯ��");
    public static Notice FILE_SIZE_OVER = new Notice("���ε� ������ ���� ũ�⸦ �ʰ��Ͽ����ϴ�.",
                                                    "���ε� ������ ���� ũ��� �ִ� 10MB�Դϴ�.",
                                                    "");
}
