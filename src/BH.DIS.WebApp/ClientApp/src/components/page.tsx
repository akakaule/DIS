/* @jsx jsx */
import * as React from "react";
import { Link, Routes, Route, useNavigate, useParams } from "react-router-dom";
import { Heading, Flex, IconButton } from "@chakra-ui/react";
import { jsx } from "@emotion/react";
import { ArrowBackIcon } from "@chakra-ui/icons";

interface IPage {
  title: string;
  offsetDimensionsHandler?: (height: number, width: number) => void;
  children: any;
  backbutton?: boolean;
  backUrl?: string;
  backIndex?: string;
}

const Page: React.FunctionComponent<IPage> = (props) => {
  const params = useParams();
  const ref = React.useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  React.useEffect(() => {
    if (ref.current)
      if (props.offsetDimensionsHandler !== undefined) {
        props.offsetDimensionsHandler(ref.current?.offsetWidth!, ref.current?.offsetHeight!);
      }
  }, [ref.current, ref.current?.clientHeight]);
  return (
    <Flex margin="0 10% 0rem" direction="column" minHeight="0" flex="1">
      <Heading as="h1" margin="2rem 0 1rem">
        {props.backbutton ? (
          <IconButton
            onClick={() => {
              navigate(props.backUrl!, { state: { tabIndex: props.backIndex } });
            }}
            aria-label="Go back"
            icon={<ArrowBackIcon verticalAlign="center" />}
            variant="unstyled"
            fontSize="var(--chakra-fontSizes-4xl)"
          />
        ) : null}{" "}
        {props.title}
      </Heading>
      <Flex ref={ref} minHeight="0" flex="1">
        {props.children}
      </Flex>
    </Flex>
  );
};

export default Page;
