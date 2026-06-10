# GitHub Reply Formats

Use these templates when posting inline replies in Step 5.
Always quote `comment_id` to prevent shell injection.
All replies must be written in Korean.

## Mention Rule

Always start every reply with `@<reviewer_username>` (the `user` field from the comment).

## VALID - fix succeeded

```text
@<reviewer_username> <abc1234> 에서 반영했습니다. (근거: <출처>)
```

## VALID - fix failed

```text
@<reviewer_username> 지적 사항이 타당합니다. 직접 수정이 필요하여 별도 처리하겠습니다.
```

## INVALID

Do not use a long rebuttal or quote repository rules verbatim.

```text
@<reviewer_username> <refutation rationale in Korean>
```

## PARTIAL - accepted

```text
@<reviewer_username> 부분적으로 타당하다고 판단하여 <abc1234> 에서 반영했습니다.
```

## PARTIAL - rejected

```text
@<reviewer_username> 검토 결과 이 방향으로는 적용하지 않기로 결정했습니다.
```

## PARTIAL - pending

```text
@<reviewer_username> 검토 중입니다. 추후 답변드리겠습니다.
```
