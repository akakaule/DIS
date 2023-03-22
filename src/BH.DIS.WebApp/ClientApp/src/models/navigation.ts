import { ReactNode } from "react";

export type Navigation = (
  | {
      name: string;
      path: string;
      header: boolean;
      render: (props: any) => (clientProps: any) => ReactNode;
    }
  | {
      name: string;
      path: string;
      header: boolean;
      render?: undefined;
    }
)[];
