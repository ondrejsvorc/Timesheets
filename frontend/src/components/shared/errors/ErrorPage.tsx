import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Routes } from "@/constants/routes";
import { Texts } from "@/constants/texts";

export const ErrorPage = () => {
  const navigate = useNavigate();

  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-xl">{Texts.notFoundTitle}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          <p className="text-sm text-muted-foreground">{Texts.notFoundDescription}</p>
          <div className="flex gap-2">
            <Button variant="default" onClick={() => navigate(Routes.projects())}>
              {Texts.goToOverview}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
