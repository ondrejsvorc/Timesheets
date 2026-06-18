import { Trash2 } from "lucide-react";
import { useRef, useState } from "react";
import { useRevalidator, useRouteLoaderData } from "react-router";
import { ConfirmationDialog } from "@/components/shared/dialogs/ConfirmationDialog";
import { SubPageHeader, SubPageTitle } from "@/components/shared/layout/SubPageHeader";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Texts } from "@/constants/texts";
import type { RootLoaderData } from "@/router";
import { addTimesheetComment, deleteTimesheetComment } from "../api";
import type { TimesheetComment } from "./Comment";
import { formatCommentDateTime } from "./commentActivity";
import { StatusChangeCommentEntry } from "./StatusChangeCommentEntry";

export interface TimesheetCommentsScope {
  employeeId: string;
  year: number;
  month: number;
}

interface TimesheetCommentsProps {
  scope: TimesheetCommentsScope;
  comments: TimesheetComment[];
}

export const TimesheetComments = ({ scope, comments }: TimesheetCommentsProps) => {
  const revalidator = useRevalidator();
  const rootData = useRouteLoaderData("root") as RootLoaderData | undefined;
  const currentUserId = rootData?.currentUser?.id;
  const MAX_COMMENT_LENGTH = 500;
  const [draft, setDraft] = useState("");
  const [isSending, setIsSending] = useState(false);
  const [sendError, setSendError] = useState<string | null>(null);
  const [commentToDelete, setCommentToDelete] = useState<string | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  const onSend = async () => {
    if (isSending) {
      return;
    }

    setIsSending(true);
    setSendError(null);
    try {
      const controller = new AbortController();
      await addTimesheetComment(
        {
          employeeId: scope.employeeId,
          year: scope.year,
          month: scope.month,
          text: draft,
        },
        controller.signal,
      );
      setDraft("");
      revalidator.revalidate();
      requestAnimationFrame(() => textareaRef.current?.focus());
    } catch (error) {
      setSendError(error instanceof Error ? error.message : Texts.sendCommentFailed);
    } finally {
      setIsSending(false);
    }
  };

  return (
    <>
      <SubPageHeader>
        <SubPageTitle>{Texts.comments}</SubPageTitle>
      </SubPageHeader>
      <div className="pb-8 space-y-4">
        {comments.length === 0 ? (
          <div className="text-sm text-muted-foreground">{Texts.noCommentsYet}</div>
        ) : (
          comments.map((comment) =>
            comment.type === "statusChange" ? (
              <StatusChangeCommentEntry key={comment.id} createdAt={comment.createdAt} statusChange={comment.statusChange} />
            ) : (
              <div key={comment.id} className="rounded-md border bg-background px-3 py-3 md:px-4 md:py-3.5">
                <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
                  <div className="text-sm font-medium text-foreground">{comment.author.name}</div>
                  <div className="flex items-center gap-1">
                    <div className="text-xs text-muted-foreground tabular-nums">{formatCommentDateTime(comment.createdAt)}</div>
                    {currentUserId === comment.author.id && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        className="h-7 w-7 text-muted-foreground hover:text-destructive"
                        aria-label={Texts.delete}
                        onClick={() => setCommentToDelete(comment.id)}
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    )}
                  </div>
                </div>
                <div className="mt-2 text-sm leading-6 text-foreground/90 whitespace-pre-wrap">{comment.text}</div>
              </div>
            ),
          )
        )}

        <div className="space-y-2">
          <Textarea
            ref={textareaRef}
            value={draft}
            onChange={(event) => setDraft(event.currentTarget.value)}
            placeholder={Texts.writeCommentPlaceholder}
            disabled={isSending}
            maxLength={MAX_COMMENT_LENGTH}
            className="w-full max-h-40 resize-none"
            onKeyDown={(event) => {
              if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                if (draft.trim().length === 0) {
                  return;
                }
                void onSend();
              }
            }}
          />
          <div className="text-xs text-muted-foreground text-right tabular-nums">
            {draft.length}/{MAX_COMMENT_LENGTH}
          </div>
          {sendError && <div className="text-sm text-destructive">{sendError}</div>}
          <div className="flex justify-end">
            <Button type="button" onClick={onSend} disabled={isSending || draft.trim().length === 0}>
              {isSending ? Texts.sending : Texts.send}
            </Button>
          </div>
        </div>
      </div>

      <ConfirmationDialog
        open={commentToDelete !== null}
        onCancel={() => setCommentToDelete(null)}
        onConfirm={async (_event, signal) => {
          if (!commentToDelete) {
            return;
          }

          try {
            await deleteTimesheetComment(
              {
                commentId: commentToDelete,
                employeeId: scope.employeeId,
                year: scope.year,
                month: scope.month,
              },
              signal,
            );
            if (!signal.aborted) {
              setCommentToDelete(null);
              revalidator.revalidate();
            }
          } catch (error) {
            setCommentToDelete(null);
            setSendError(error instanceof Error ? error.message : Texts.deleteCommentFailed);
          }
        }}
      />
    </>
  );
};
