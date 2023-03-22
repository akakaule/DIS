import * as React from "react";
import * as api from "api-client";
import { useToast } from "@chakra-ui/react"

export type toastProperties = {display: boolean | undefined, status: "success" | "error", title: string, description: string}

export interface IFeedbackToastProps {
    values: toastProperties
}

export default function FeedbackToast(props: IFeedbackToastProps) {
    const toast = useToast();

    React.useEffect(() => {
        displayToast();
    }, [props])

    const displayToast = () => {

        if(props.values.display){
            toast({
                title: props.values.title,
                description: props.values.description,
                status: props.values.status,
                isClosable: true,
                duration: 3000
            });
        }
        
    }

    return (
        <></>
    )
}