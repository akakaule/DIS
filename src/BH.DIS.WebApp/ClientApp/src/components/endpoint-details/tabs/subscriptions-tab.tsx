import * as React from "react";
import * as api from "api-client";
import { Dispatch, SetStateAction, useState } from "react";
import { useParams } from "react-router-dom";
import { ITableHeadCell } from "components/data-table/data-table-header";
import DataTable, { ITableHeadAction, ITableRow } from "components/data-table/data-table";
import FeedbackToast, { toastProperties } from "../feedback-toast";
import { Box } from "@material-ui/core";
import AlertModal from "../alert-modal";

interface ISubscriptionsTabProps {
    setIsTabEnabled: React.Dispatch<React.SetStateAction<boolean>>;
    roleAssignmentScript: string;
}

const SubscriptionsTab = (props: ISubscriptionsTabProps) => {
    const client = new api.Client(api.CookieAuth());
    const params = useParams();

    const [subscriptions, setSubscriptions] = React.useState<api.EndpointSubscription[]>([]);
    const [selectedSubscription, setSelectedSubscription] = React.useState<api.EndpointSubscription>();
    const [isSubscriptionModalOpen, setIsSubscriptionModalOpen] = React.useState<boolean>(false);

    const [rows, setRows] = React.useState<ITableRow[]>([]);

    const [feedbackToast, setFeedbackToast] = React.useState<toastProperties>({ display: undefined, status: "success", description: "", title: "" });

    React.useEffect(() => {
        const fetchData = async () => {
            const tempSubscriptions = await client.getEndpointSubscribe(params.id!);

            setSubscriptions(tempSubscriptions);

            if (tempSubscriptions.length > 0)
                props.setIsTabEnabled(true);
            else
                props.setIsTabEnabled(false);
        }

        fetchData()
    }, [props]);

    React.useEffect(() => {
        setRows(mapSubscriptions())
    }, [subscriptions]);

    const closeSubscriptionModal = () => {
        setIsSubscriptionModalOpen(false);
    }

    const doActionSelectedRows = (iTableRows: ITableRow[], actionName: string) => {
        iTableRows.forEach((row) => {
            const action = row.bodyActions?.find((a) => a.name === actionName);
            action?.onClick.call("");
        });
    }

    function mapSubscriptions() {
        const tempRows: ITableRow[] = subscriptions.map(sub => {
            let mailOrUrl = sub.mail!
            if (!mailOrUrl) { mailOrUrl = sub.url! }
            const row: ITableRow = {
                id: sub.id!,
                bodyActions: [
                    {
                        name: "Info",
                        onClick: () => {
                            setSelectedSubscription(sub);
                            setIsSubscriptionModalOpen(true);
                            return true;
                        }
                    },
                    {
                        name: "Delete",
                        onClick: () => {
                            deleteSubscription(sub);
                            return false;
                        },
                    },
                ],
                hoverText: mailOrUrl!,
                data: new Map([
                    ["author", { value: sub.authorId!, searchValue: sub.authorId! }],
                    ["mail", { value: mailOrUrl!, searchValue: mailOrUrl! }],
                    ["type", { value: sub.type!, searchValue: sub.type! }]
                ])
            }
            return row;
        });
        return tempRows;
    }

    async function deleteSubscription(subscription: api.Subscription, requester?: string): Promise<void> {
        removeFromTable(subscription);
        await client.deleteEndpointSubscribe(params.id!, new api.SubscriptionAuthor({ id: subscription.id, author: subscription.authorId })).
            then(async (res) => {
                client.getEndpointSubscribe(params.id!)
                    .then(sub => {
                        setSubscriptions(sub);
                        setFeedbackToast({ display: true, status: "success", description: "Subscription deleted successfully", title: "Successfully deleted" });
                    });

            }).catch(() => {
                setFeedbackToast({ display: true, status: "error", description: "Unable to delete Subscription. Logged in user is not owner of subscription.", title: "Unable to delete" });
            });
    }

    const removeFromTable = (event: api.Subscription) => {
        const index = subscriptions?.indexOf(event);
        if (index !== undefined && index > -1) {
            subscriptions?.splice(index, 1);
        }
    }

    const headCells: ITableHeadCell[] = [
        { id: "author", label: "Author", numeric: false },
        { id: "mail", label: "Email/Url", numeric: false },
        { id: "type", label: "Type", numeric: false },
    ];

    const headActions: ITableHeadAction[] = [
        {
            name: "Delete",
            onClick: (selectedRows: ITableRow[]) => {
                doActionSelectedRows(selectedRows, "Delete");
                return false;
            },
        },
    ];

    return (
        <Box width="100%" marginRight="1rem" display="flex" flexDirection="column">
            <DataTable
                headCells={headCells}
                headActions={headActions}
                rows={rows}
                withCheckboxes={true}
                noDataMessage="No Subscriptions available"
                isLoading={false}
                count={subscriptions.length ?? 0}
                subscribe={params.id!}
                hideDense={true}
                roleAssignmentScript={props.roleAssignmentScript}
                fixedWidth={"-webkit-fill-available"}
            />
            <AlertModal isOpen={isSubscriptionModalOpen} onClose={closeSubscriptionModal} subscription={selectedSubscription} endpointId={params.id!} editable={false}></AlertModal>
            <FeedbackToast values={feedbackToast} />
        </Box>
    );
}

export default SubscriptionsTab;
