import * as React from 'react';
import { Button, Modal, Form } from 'react-bootstrap';

import ensureCollectPopup, { IPopupsState } from './reducer';

import Spinner from '../../bootCommon/spinner';

type ConnectedProps = IPopupsState;

type popupOptions = {
    readonly id: string;
    readonly header?: string;
    readonly cancelText?: string;
    readonly OKText?: string;
    readonly size?: any;
};

type ComponentProps = popupOptions;

const PopupView: React.FunctionComponent<ConnectedProps & ComponentProps & { dispatch }> = ({ currentPopups, id, header, cancelText, OKText, dispatch, children, size }) => {

    return <div>
        {currentPopups && currentPopups[id] && <Modal show={true} size={size||'sm'}
            onHide={() => dispatch(ensureCollectPopup().closePopup(id, true))}>
            <Modal.Header closeButton>
                <Modal.Title>{header || 'Missing Data'}</Modal.Title>
            </Modal.Header>

            <Form onSubmit={e => {
                e.preventDefault();
                dispatch(ensureCollectPopup().closePopup(id));
            }} >
                <Modal.Body>
                    {children}
                </Modal.Body>

                <Modal.Footer>

                    <Spinner isLoading={!!currentPopups[id].saving} prompt="Saving">
                    </Spinner>

                    {currentPopups[id].err && <h3 className="text-danger">{currentPopups[id].err}</h3>}

                    <Button variant="secondary" disabled={!!currentPopups[id].saving}
                        onClick={() => dispatch(ensureCollectPopup().closePopup(id, true))}
                    >
                        {cancelText || 'cancel'}
                    </Button>

                    <Button variant="primary" type="submit" disabled={!!currentPopups[id].saving}>
                        {OKText || 'OK'}
                    </Button>

                </Modal.Footer>
            </Form>

        </Modal>
        }
    </div>;
}

import { connect } from 'react-redux';
const PopupViewConnected = connect<ConnectedProps, { dispatch }, ComponentProps>((state) => {
    return ensureCollectPopup().getCurrentState(state);

})(PopupView);


export function withPopup<T>(options: popupOptions, WrappedComponent: React.ComponentType<T>) {
    const displayName =
        WrappedComponent.displayName || WrappedComponent.name || "Component";

    return class ComponentWithPopup extends React.Component<T> {

        public static displayName = `withPopup(${displayName})`;

        public render() {

            return <PopupViewConnected {...options}>
                <WrappedComponent {...(this.props as T)}/>
            </PopupViewConnected>;
        }
    };
}